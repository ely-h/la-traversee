using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyQrCodeDisplay : MonoBehaviour
{
    [SerializeField] private RawImage qrCodeImage;
    [SerializeField] private TMP_Text fallbackUrlText;
    [SerializeField] private int qrSize = 256;

    public string RefreshQrCode(int port)
    {
        string url = BuildJoinUrl(port);

        if (fallbackUrlText != null)
        {
            fallbackUrlText.text = url;
        }

        Texture2D qrTexture = TryGenerateQrTexture(url);
        if (qrCodeImage != null)
        {
            qrCodeImage.texture = qrTexture;
            qrCodeImage.enabled = qrTexture != null;
        }

        if (qrTexture == null)
        {
            Debug.LogWarning("LobbyQrCodeDisplay: ZXing.Net n'est pas installe. Importez la librairie puis relancez Unity.");
        }

        return url;
    }

    private string BuildJoinUrl(int port)
    {
        string ip = GetLocalIPv4Address();
        return $"http://{ip}:{port}";
    }

    private string GetLocalIPv4Address()
    {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            IPInterfaceProperties properties = networkInterface.GetIPProperties();
            UnicastIPAddressInformation address = properties.UnicastAddresses
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                      !IPAddress.IsLoopback(ip.Address) &&
                                      !ip.Address.ToString().StartsWith("169.254."));

            if (address != null)
            {
                return address.Address.ToString();
            }
        }

        return "127.0.0.1";
    }

    private Texture2D TryGenerateQrTexture(string content)
    {
        Type writerType = FindType("ZXing.BarcodeWriterPixelData");
        Type formatType = FindType("ZXing.BarcodeFormat");
        Type qrOptionsType = FindType("ZXing.QrCode.QrCodeEncodingOptions");

        if (writerType == null || formatType == null || qrOptionsType == null)
        {
            return null;
        }

        object writer = Activator.CreateInstance(writerType);
        object options = Activator.CreateInstance(qrOptionsType);

        qrOptionsType.GetProperty("Width")?.SetValue(options, qrSize);
        qrOptionsType.GetProperty("Height")?.SetValue(options, qrSize);
        qrOptionsType.GetProperty("Margin")?.SetValue(options, 1);

        writerType.GetProperty("Format")?.SetValue(writer, Enum.Parse(formatType, "QR_CODE"));
        writerType.GetProperty("Options")?.SetValue(writer, options);

        MethodInfo writeMethod = writerType.GetMethod("Write", new[] { typeof(string) });
        object pixelData = writeMethod?.Invoke(writer, new object[] { content });
        if (pixelData == null)
        {
            return null;
        }

        int width = (int)pixelData.GetType().GetProperty("Width").GetValue(pixelData);
        int height = (int)pixelData.GetType().GetProperty("Height").GetValue(pixelData);
        byte[] pixels = (byte[])pixelData.GetType().GetProperty("Pixels").GetValue(pixelData);

        Color32[] colors = new Color32[width * height];
        for (int i = 0, p = 0; i < pixels.Length; i += 4, p++)
        {
            colors[p] = new Color32(pixels[i + 2], pixels[i + 1], pixels[i], pixels[i + 3]);
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels32(colors);
        texture.Apply();
        return texture;
    }

    private Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName, false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}

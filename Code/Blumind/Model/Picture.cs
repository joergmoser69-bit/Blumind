using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;
using Blumind.Controls;
using Blumind.Core;
using Blumind.Globalization;

namespace Blumind.Model
{
    public class Picture
    {
        static readonly HttpClient ImageClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
            MaxResponseContentBufferSize = 32 * 1024 * 1024
        };
        private string _ID;
        private string _Url;
        private string _Name;
        private Image _Data;

        public Picture()
        {
        }

        public Picture(string name, Image image)
        {
            Name = name;
            Data = image;
        }

        public string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public string Url
        {
            get { return _Url; }
            set { _Url = value; }
        }

        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        public Image Data
        {
            get { return _Data; }
            set { _Data = value; }
        }

        public static Image LoadImageFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            if(MyIconLibrary.Share.ContainsKey(url))
            {
                return LoadImageFromLibrary(url);
            }

            var uri = new Uri(url);
            if (uri.IsFile)
            {
                return LoadImageFromFile(url);
            }
            else
            {
                return LoadImageFromWeb(url);
            }
        }
        
        public static Image LoadImageFromWeb(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                byte[] buffer = ImageClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                if (buffer != null && buffer.Length > 0)
                {
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        using var decoded = Image.FromStream(stream);
                        return (Image)decoded.Clone();
                    }
                }
            }

            return null;
        }

        public static Image LoadImageFromWeb(string url, bool limitImageSize)
        {
            Image image = LoadImageFromWeb(url);
            if (image != null && limitImageSize)
                return PaintHelper.CreateThumbImage(image, 800, 600);
            else
                return image;
        }

        public static Image LoadImageFromLibrary(string libID)
        {
            if (MyIconLibrary.Share.ContainsKey(libID))
            {
                Image image = ((Picture)MyIconLibrary.Share[libID]).Data;
                if (image != null)
                    return (Image)image.Clone();
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        public static Image LoadImageFromFile(string filename)
        {
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                throw new FileNotFoundException();

            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return Image.FromStream(stream);
            }
        }

        public static OpenFileDialog GetOpenFileDialog()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = string.Format("{0}|*.png;*.jpeg;*.jpg;*.gif;*.bmp",
                Lang._("All Image Files"));

            return dialog;
        }
    }
}

using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using NewLife.Threading;
using NewLife.Web;
using NewLife.Log;
using System.Diagnostics;

namespace NewLife.IP
{
    /// <summary>IP����</summary>
    public static class Ip
    {
        private static object lockHelper = new object();
        private static Zip zip;

        private static String _DbFile;
        /// <summary>�����ļ�</summary>
        public static String DbFile { get { return _DbFile; } set { _DbFile = value; zip = null; } }

        static Ip()
        {
            var ns = new String[] { "qqwry.dat", "qqwry.gz", "ip.gz", "ip.gz.config", "ipdata.config" };
            foreach (var item in ns)
            {
                var fs = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, item, SearchOption.AllDirectories);
                if (fs != null && fs.Length > 0)
                {
                    _DbFile = Path.GetFullPath(fs[0]);
                    break;
                }
            }

            // �������û��IP���ݿ⣬�����������
            if (_DbFile.IsNullOrWhiteSpace())
            {
                ThreadPoolX.QueueUserWorkItem(() =>
                {
                    var url = "http://www.newlifex.com/showtopic-51.aspx";
                    XTrace.WriteLine("û���ҵ�IP���ݿ⣬׼��������ȡ {0}", url);

                    var client = new WebClientX();

                    var sw = new Stopwatch();
                    sw.Start();
                    var file = client.DownloadLink(url, "ip.gz", "App_Data".GetFullPath());
                    sw.Stop();

                    XTrace.WriteLine("����IP���ݿ���ɣ���{0:n0}�ֽڣ���ʱ{1}����", file.AsFile().Length, sw.ElapsedMilliseconds);
                });
            }
        }

        static Boolean Init()
        {
            if (zip != null) return true;
            lock (typeof(Ip))
            {
                if (zip != null) return true;

                var z = new Zip();

                if (!File.Exists(_DbFile))
                {
                    //throw new InvalidOperationException("�޷��ҵ�IP���ݿ�" + _DbFile + "��");
                    XTrace.WriteLine("�޷��ҵ�IP���ݿ�{0}", _DbFile);
                    return false;
                }
                using (var fs = File.OpenRead(_DbFile))
                {
                    z.SetStream(fs);
                }
                zip = z;
            }

            if (zip.Stream == null) throw new InvalidOperationException("�޷���IP���ݿ�" + _DbFile + "��");
            return true;
        }

        /// <summary>��ȡIP��ַ</summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static String GetAddress(String ip)
        {
            if (String.IsNullOrEmpty(ip)) return "";

            if (!Init()) return "";

            var ip2 = IPToUInt32(ip.Trim());
            lock (lockHelper)
            {
                var addr = zip.GetAddress(ip2);
                //if (String.IsNullOrEmpty(addr) || addr.IndexOf("IANA") >= 0) return "";
                if (String.IsNullOrEmpty(addr)) return "";

                return addr;
            }
        }

        static uint IPToUInt32(String IpValue)
        {
            var ss = IpValue.Split('.');
            var buf = new Byte[4];
            for (int i = 0; i < 4; i++)
            {
                var n = 0;
                if (i < ss.Length && Int32.TryParse(ss[i], out n))
                {
                    buf[3 - i] = (Byte)n;
                }
            }
            return BitConverter.ToUInt32(buf, 0);
        }
    }
}
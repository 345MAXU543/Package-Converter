using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Package_Converter
{
    public partial class MainWindow : Window
    {
        List<originalPackage> originalPackage_list = new List<originalPackage>();

        class originalPackage
        {
            public int index { get; set; }
            public string time { get; set; }
            public string isRxD { get; set; }
            public string data { get; set; }
            public string is32bit { get; set; }
        }

        public class BitInfo
        {
            public string ByteIndex { get; set; }
            public string Hex { get; set; }

            public string b7 { get; set; }
            public string b6 { get; set; }
            public string b5 { get; set; }
            public string b4 { get; set; }
            public string b3 { get; set; }
            public string b2 { get; set; }
            public string b1 { get; set; }
            public string b0 { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyBitColorTemplate();
            ApplyRxDColorTemplate();
        }

        private void btnPastePackage_Click(object sender, RoutedEventArgs e)
        {
            txt_OringePackage.Paste();
            fn_PackageSerilize();
        }

        private void btnClearPackage_Click(object sender, RoutedEventArgs e)
        {
            txt_OringePackage.Clear();
            lst_Package.ItemsSource = null;
            lst_Package.Items.Clear();
        }

        private void fn_PackageSerilize()
        {
            try
            {
                if (txt_OringePackage.Text.Length == 0)
                    return;

                originalPackage_list.Clear();

                string O_Package = txt_OringePackage.Text;
                string[] O_Package_lines = O_Package.Split(
                    new string[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries);

                originalPackage lastPackge = null;
                int index = 0;

                foreach (string line in O_Package_lines)
                {
                    originalPackage package = new originalPackage();

                    // ---- 解析時間 ----
                    string time = line.Split('x')[0].Trim().Split(' ')[0];
                    package.time = time;

                    // ---- 解析 data ----
                    string raw = line.Split('x')[1].Replace("D : ", "").Trim();
                    package.data = raw;

                    // ---- 判斷 bit 數 ----
                    string firstTwo = raw.Replace(" ", "").Substring(0, 2);
                    ushort firstByte = Convert.ToUInt16(firstTwo, 16);

                    if (firstByte > 0x3F)
                        package.is32bit = "24 bit";
                    else
                        package.is32bit = "32 bit";

                    // ---- 判斷 Rx / Tx ----
                    if (line.Contains("R"))
                    {
                        package.isRxD = "RxD";
                        package.index = index;
                    }
                    else if (line.Contains("T"))
                    {
                        package.isRxD = "TxD";
                        package.index = index;
                    }
                    else
                    {
                        if (lastPackge != null)
                        {
                            package.index = lastPackge.index;
                            package.time = " ";
                            package.isRxD = lastPackge.isRxD;
                        }
                    }

                    originalPackage_list.Add(package);
                    lastPackge = package;
                    index++;
                }

                lst_Package.ItemsSource = null;
                lst_Package.ItemsSource = originalPackage_list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("解析封包時發生錯誤：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ⭐⭐⭐ Bits 欄位變色（24 bit = 橘色 / 32 bit = 綠色）
        private void ApplyBitColorTemplate()
        {
            if (lst_Package.View is GridView gv)
            {
                GridViewColumn bitCol = gv.Columns[4]; // 最右邊 32bit 欄位

                FrameworkElementFactory tb = new FrameworkElementFactory(typeof(TextBlock));
                tb.SetBinding(TextBlock.TextProperty, new Binding("is32bit"));

                Style style = new Style(typeof(TextBlock));
                tb.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                tb.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                // 32bit 預設 → 綠色
                style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.LightGreen));

                // 24bit → 橘色
                DataTrigger trig24 = new DataTrigger
                {
                    Binding = new Binding("is32bit"),
                    Value = "24 bit"
                };
                trig24.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Red));
                style.Triggers.Add(trig24);

                tb.SetValue(TextBlock.StyleProperty, style);

                DataTemplate dt = new DataTemplate();
                dt.VisualTree = tb;

                bitCol.CellTemplate = dt;
            }
        }

        private void ApplyRxDColorTemplate()
        {
            if (lst_Package.View is GridView gv)
            {
                GridViewColumn rtdCol = gv.Columns[2]; // R/T 欄位是第 3 欄

                FrameworkElementFactory tb = new FrameworkElementFactory(typeof(TextBlock));
                tb.SetBinding(TextBlock.TextProperty, new Binding("isRxD"));

                tb.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                tb.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

                Style style = new Style(typeof(TextBlock));

                // 預設是 TxD → 藍色
                style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.LightBlue));

                // 如果是 RxD → 紅色
                DataTrigger trigRx = new DataTrigger
                {
                    Binding = new Binding("isRxD"),
                    Value = "RxD"
                };
                trigRx.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.GreenYellow));

                style.Triggers.Add(trigRx);

                tb.SetValue(TextBlock.StyleProperty, style);

                DataTemplate dt = new DataTemplate();
                dt.VisualTree = tb;

                rtdCol.CellTemplate = dt;
            }
        }

        private void txt_OringePackage_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                fn_PackageSerilize();
            }
        }



        private void lst_Package_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            originalPackage SelPackage = new originalPackage();
            if (lst_Package.SelectedItem is originalPackage pkg) SelPackage = pkg;
            else SelPackage = null;

            if (SelPackage != null)
            {
                LoadBits(SelPackage.data);
            }

        }

        List<BitInfo> bits = new List<BitInfo>();
        List<BitInfo> bits_CMD = new List<BitInfo>();
        List<BitInfo> reCal_bits = new List<BitInfo>();
        List<BitInfo> reCal_bits_CMD = new List<BitInfo>();
        private void LoadBits(string hexData)
        {
            bits.Clear();
            bits_CMD.Clear();
            dgBits.ItemsSource = null;
            dgBits_CMD.ItemsSource = null;

            string[] bytes = hexData.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = bytes.Length-1; i >= 0; i--)
            {
                string hex = bytes[i];
                byte value = Convert.ToByte(hex, 16);
                string bin = Convert.ToString(value, 2).PadLeft(8, '0'); // 8-bit binary


                if (i > 0)
                {
                    bits.Add(new BitInfo
                    {
                        ByteIndex = (bytes.Length-i ).ToString(),
                        Hex = hex,

                        b7 = bin[0].ToString(),
                        b6 = bin[1].ToString(),
                        b5 = bin[2].ToString(),
                        b4 = bin[3].ToString(),
                        b3 = bin[4].ToString(),
                        b2 = bin[5].ToString(),
                        b1 = bin[6].ToString(),
                        b0 = bin[7].ToString(),
                    });



                }
                else
                {
                    bits_CMD.Add(new BitInfo
                    {
                        ByteIndex = (bytes.Length - i ).ToString(),
                        Hex = hex,
                        b7 = bin[0].ToString(),
                        b6 = bin[1].ToString(),
                        b5 = bin[2].ToString(),
                        b4 = bin[3].ToString(),
                        b3 = bin[4].ToString(),
                        b2 = bin[5].ToString(),
                        b1 = bin[6].ToString(),
                        b0 = bin[7].ToString(),
                    });
                }


            }

            dgBits.ItemsSource = bits;
            dgBits_CMD.ItemsSource = bits_CMD;
            BuildDataMatrix(bytes);


        }


        //public class BitMatrixRow
        //{
        //    public string Byte { get; set; }
        //    public string Hex { get; set; }
        //    public string b7 { get; set; }
        //    public string b6 { get; set; }
        //    public string b5 { get; set; }
        //    public string b4 { get; set; }
        //    public string b3 { get; set; }
        //    public string b2 { get; set; }
        //    public string b1 { get; set; }
        //    public string b0 { get; set; }
        //}

        private void BuildDataMatrix(string[] bytes)
        {
            reCal_dgBits.ItemsSource = null;   // ← 你自己的 DataGrid 名稱
            List<BitInfo> matrix = new List<BitInfo>();
            List<BitInfo> matrix_CMD = new List<BitInfo>();


            // ------------ 判斷封包種類 ------------
            if (bytes.Length == 6)
            {
                // ======================
                //         32 bit
                // ======================

                string[] bins = new string[6];
                for (int i = 0; i < 6; i++)
                {
                    bins[i] = Convert.ToString(Convert.ToByte(bytes[i], 16), 2).PadLeft(8, '0');
                }

                string strCMD = bins[0].Substring(2, 6);
                matrix_CMD.Add( new BitInfo
                {
                    Hex =  Convert.ToByte(strCMD, 2).ToString("X2"),
                    b7 = "0",
                    b6 = "0",
                    b5 = strCMD[0].ToString(),
                    b4 = strCMD[1].ToString(),
                    b3 = strCMD[2].ToString(),
                    b2 = strCMD[3].ToString(),
                    b1 = strCMD[4].ToString(),
                    b0 = strCMD[5].ToString(),
                });
                reCal_dgBits_CMD.ItemsSource = matrix_CMD;

                string all =
                    bins[5].Substring(4, 4) +     // d31 ~ d28
                    bins[4].Substring(1, 7) +     // d27 ~ d21
                    bins[3].Substring(1, 7) +     // d20 ~ d14
                    bins[2].Substring(1, 7) +     // d13 ~ d7
                    bins[1].Substring(1, 7);      // d6 ~ d0

                for (int row = 0; row < 4; row++)
                {
                    int s = row * 8;

                    string bit8 = all.Substring(s, 8);
                    string hex = Convert.ToByte(bit8, 2).ToString("X2");

                    matrix.Add(new BitInfo
                    {
                        Hex = hex,
                        b7 = all[s + 0].ToString(),
                        b6 = all[s + 1].ToString(),
                        b5 = all[s + 2].ToString(),
                        b4 = all[s +3 ].ToString(),
                        b3 = all[s + 4].ToString(),
                        b2 = all[s +5 ].ToString(),
                        b1 = all[s +6 ].ToString(),
                        b0 = all[s + 7].ToString(),
                    });
                }
            }
            else if (bytes.Length == 5)
            {
                // ======================
                //         24 bit
                // ======================

                string[] bins = new string[5];
                for (int i = 0; i < 5; i++)
                {
                    bins[i] = Convert.ToString(Convert.ToByte(bytes[i], 16), 2).PadLeft(8, '0');
                }


                string strCMD = bins[0].Substring(2, 6);
                matrix_CMD.Add(new BitInfo
                {
                    Hex = Convert.ToByte(strCMD, 2).ToString("X2"),
                    b7 = "0",
                    b6 = "0",
                    b5 = strCMD[0].ToString(),
                    b4 = strCMD[1].ToString(),
                    b3 = strCMD[2].ToString(),
                    b2 = strCMD[3].ToString(),
                    b1 = strCMD[4].ToString(),
                    b0 = strCMD[5].ToString(),
                });
                reCal_dgBits_CMD.ItemsSource = matrix_CMD;

                string all =
                    bins[4].Substring(5, 3) +     // d23 d22 d21
                    bins[3].Substring(1, 7) +     // d20~d14
                    bins[2].Substring(1, 7) +     // d13~d7
                    bins[1].Substring(1, 7);      // d6~d0

                for (int row = 0; row < 3; row++)
                {
                    int s = row * 8;

                    string bit8 = all.Substring(s, 8);
                    string hex = Convert.ToByte(bit8, 2).ToString("X2");

                    matrix.Add(new BitInfo
                    {
                        Hex = hex,
                        b7 = all[s + 0].ToString(),
                        b6 = all[s + 1].ToString(),
                        b5 = all[s + 2].ToString(),
                        b4 = all[s + 3].ToString(),
                        b3 = all[s + 4].ToString(),
                        b2 = all[s + 5].ToString(),
                        b1 = all[s + 6].ToString(),
                        b0 = all[s + 7].ToString(),
                    });
                }
            }


            reCal_dgBits.ItemsSource = matrix;  // ← 正確顯示位置
        }



        private void btn_ReCal_Click(object sender, RoutedEventArgs e)
        {
            fn_PackageSerilize();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoreanSoftKeyboard.WPF
{
    public class KeyModel
    {
        public int VK { get; }
        public string English { get; }
        public string Korean { get; }
        public string KoreanShift { get; }
        public bool IsControl { get; }
        public bool IsWide { get; }


        public KeyModel(int vk, string eng, string kor, string korShift, bool isControl = false, bool isWide = false)
        {
            VK = vk; English = eng; Korean = kor; KoreanShift = korShift; IsControl = isControl; IsWide = isWide;
        }
    }
}

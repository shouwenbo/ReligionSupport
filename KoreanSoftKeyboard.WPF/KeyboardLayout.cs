using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoreanSoftKeyboard.WPF
{
    public class KeyboardLayout
    {
        // VK constants used are simplified. 请根据需要扩展或替换为完整的虚拟键值
        public static List<List<KeyModel>> DefaultLayout()
        {
            return new List<List<KeyModel>>
            {
                // 第一行：数字行
                new List<KeyModel>
                {
                    new KeyModel(192,"`","`","~", false),
                    new KeyModel(49,"1","1","!", false),
                    new KeyModel(50,"2","2","@", false),
                    new KeyModel(51,"3","3","#", false),
                    new KeyModel(52,"4","4","$", false),
                    new KeyModel(53,"5","5","%", false),
                    new KeyModel(54,"6","6","^", false),
                    new KeyModel(55,"7","7","&", false),
                    new KeyModel(56,"8","8","*", false),
                    new KeyModel(57,"9","9","(", false),
                    new KeyModel(48,"0","0",")", false),
                    new KeyModel(189,"-","-","_", false),
                    new KeyModel(187,"=","=","+", false),
                    new KeyModel(8,"Back","", "", true)
                },
                // 第二行：Q..P 行
                new List<KeyModel>
                {
                    new KeyModel(9,"Tab","", "", true),
                    new KeyModel(81,"Q","ㅂ","ㅃ", false),
                    new KeyModel(87,"W","ㅈ","ㅉ", false),
                    new KeyModel(69,"E","ㄷ","ㄸ", false),
                    new KeyModel(82,"R","ㄱ","ㄲ", false),
                    new KeyModel(84,"T","ㅅ","ㅆ", false),
                    new KeyModel(89,"Y","ㅛ","", false),
                    new KeyModel(85,"U","ㅕ","", false),
                    new KeyModel(73,"I","ㅑ","", false),
                    new KeyModel(79,"O","ㅐ","ㅒ", false),
                    new KeyModel(80,"P","ㅔ","ㅖ", false),
                    new KeyModel(219,"[","","", false),
                    new KeyModel(221,"]","","", false),
                    new KeyModel(220,"\\","","", true)
                },
                // 第三行：A..L + Enter
                new List<KeyModel>
                {
                    new KeyModel(20,"Caps","", "", true),
                    new KeyModel(65,"A","ㅁ","", false),
                    new KeyModel(83,"S","ㄴ","", false),
                    new KeyModel(68,"D","ㅇ","", false),
                    new KeyModel(70,"F","ㄹ","", false),
                    new KeyModel(71,"G","ㅎ","", false),
                    new KeyModel(72,"H","ㅗ","", false),
                    new KeyModel(74,"J","ㅓ","", false),
                    new KeyModel(75,"K","ㅏ","", false),
                    new KeyModel(76,"L","ㅣ","", false),
                    new KeyModel(186,";","","", false),
                    new KeyModel(222,"'","","", false),
                    new KeyModel(13,"Enter","", "", true, isWide:true)
                },
                // 第四行：Z..M + Shift
                new List<KeyModel>
                {
                    new KeyModel(16,"Shift","", "", true, isWide:true), // 左Shift
                    new KeyModel(90,"Z","ㅋ","", false),
                    new KeyModel(88,"X","ㅌ","", false),
                    new KeyModel(67,"C","ㅊ","", false),
                    new KeyModel(86,"V","ㅍ","", false),
                    new KeyModel(66,"B","ㅂ","", false),
                    new KeyModel(78,"N","ㅈ","", false),
                    new KeyModel(77,"M","ㅁ","", false),
                    new KeyModel(188,",","", "", false),
                    new KeyModel(190,".","", "", false),
                    new KeyModel(191,"/","", "", false),
                    new KeyModel(16,"ShiftR","", "", true, isWide:true) // 右Shift（VK重复为示例）
                },
                // 第五行：功能键 + 空格
                new List<KeyModel>
                {
                    new KeyModel(17,"Ctrl","", "", true),
                    new KeyModel(91,"Win","", "", true),
                    new KeyModel(18,"Alt","", "", true),
                    new KeyModel(32,"Space"," ", " ", true, isWide:true)
                }
            };
        }
    }
}

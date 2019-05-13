using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
namespace OCR.Controllers
{
    public class OCRController : Controller
    {
        public ActionResult number()
        {
            return View();
        }


        /// <summary>
        /// 图片宽度
        /// </summary>
        public int width { get; set; }
        /// <summary>
        /// 图片高度
        /// </summary>
        public int height { get; set; }
        int startX = -1, startY = -1, endX = -1, endY = -1;//四个角的坐标
        int regionNumber = 9;//每行拆分区域个数 6x6


        /// <summary>
        /// 第一步： 获取构成数字图案的主体像素集合，获取图像像素信息
        /// </summary>
        /// <returns></returns>
        public List<List<object>> MainPixel(Bitmap bitmap)
        {
            //初始化数据
            width = bitmap.Width;
            height = bitmap.Height;
            startX = -1; startY = -1; endX = -1; endY = -1;
            List<List<object>> resList = new List<List<object>>();
            List<List<object>> pixelList = new List<List<object>>();//记录图片中所有的像素信息
            Dictionary<string, int> pixelDic = new Dictionary<string, int>();
            pixelDic.Add("R", 0);
            pixelDic.Add("G", 0);
            pixelDic.Add("B", 0);
            int minRed = -1;
            int rgbGap = 50;//允许的主体颜色偏差值
            Color mainColor = new Color();//主体像素的颜色
            //循环所有图片中的所有像素
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = bitmap.GetPixel(x, y);//根据x,y坐标获取指定的颜色
                    if (c.A > 0 && !(c.R == 255 && c.G == 255 && c.B == 255))//过滤掉纯白色 RGB(255,255,255) 。【暂不考虑黑底白字】
                    {
                        if (minRed == -1 || c.R < minRed)
                            minRed = c.R;//取最小值
                        int rgbvag = (c.R + c.G + c.B) / 3;
                        c = Color.FromArgb(255, rgbvag, rgbvag, rgbvag);
                        //记录所有像素信息(坐标、颜色)
                        List<object> pixelColor = new List<object>();
                        pixelColor.Add(x);//x坐标
                        pixelColor.Add(y);//y坐标
                        pixelColor.Add(c);//颜色
                        pixelList.Add(pixelColor);
                    }

                }
            }
            //根据出现次数最多的颜色的RGB值，找出构成数字图案的主体像素
            foreach (var item in pixelList)
            {
                int x = Convert.ToInt32(item[0]);//x坐标
                int y = Convert.ToInt32(item[1]);//y坐标
                Color c = (dynamic)item[2];//颜色

                //主体像素
                //if (c.R < minRed + rgbGap)
                //{
                if (startY == -1 || y < startY)
                    startY = y;
                if (startX == -1 || x < startX)
                    startX = x;
                if (x > endX)
                    endX = x;
                if (y > endY)
                    endY = y;
                //记录主体像素信息
                List<object> pixelColor = new List<object>();
                pixelColor.Add(x);//x坐标
                pixelColor.Add(y);//y坐标
                pixelColor.Add(c);//颜色
                resList.Add(pixelColor);
                //}
            }
            width = endX - startX + 1;//去除空白区域后重新计算图片宽度
            height = endY - startY + 1;//去除空白区域后重新计算图片高度
            return resList;
        }


        /// <summary>
        /// 第二步：验证特征，排除不匹配的数字
        /// </summary>
        /// <param name="dic">主体像素</param>
        /// <returns></returns>
        public Dictionary<int, int> FeaturesCheck(Dictionary<string, int> dic)
        {
            Dictionary<int, int> numDic = new Dictionary<int, int>();
            numDic[0] = 0;
            numDic[1] = 0;
            numDic[2] = 0;
            numDic[3] = 0;
            numDic[4] = 0;
            numDic[5] = 0;
            numDic[6] = 0;
            numDic[7] = 0;
            numDic[8] = 0;
            numDic[9] = 0;
            bool valTrue = false;//是否为主体像素
            int centre = regionNumber / 2;//中间值
            bool centreTrue = false;//中心位置是否存在主体像素
            int nowCount = 0;
            int zeroRowOne = 0;//第一列
            int zeroRowLast = 0;//最后一列
            int twoTop1RightCell = 0;//第一行最右像素位置
            int twoTop2RightCell = 0;//第二行最右像素位置
            int twoLeftCell = 0;//左边的主体像素个数
            int twoRightMax = 0;//右边最大的值
            int twoRightLastMax1 = 0;//最后两行右边最大的值
            int twoRightLastMax2 = 0;//最后两行右边最大的值
            int twoLeftMin1 = 0;//左边最小
            int twoLeftMin2 = 0;//左边最小
            int twoLastCell = 0;//最后一行的主体像素
            int twoTopLastCell = 0;//第一行和最后一行总列数
            int threeCLeftCount = 0;//中心最左边列位置
            int threeLastLeftCell = 0;//最后一行最左边的位置
            int threeLastLeft1Cell = 0;//倒数第二行最左边位置
            int threeLastRight = 0;//最后一行最右
            int threeLastRight1 = 0;//倒数第二行最右
            int threeLastRight2 = 0;//倒数第三行最右
            int threeTopCell = 0;//第一行像素个数
            int threeLastCell = 0;//最后一行像素个数
            int threeLongitudinal = 0;//纵向间隔数
            int threeLongitudinalTop = 0;//纵向
            int threeLongitudinalBottom = 0;//纵向
            int fourLastCount = 0;//最后一行总像素个数
            int fiveOneCell = 0;//第一列
            int sixRightCell = 0;//右边列数
            int sixLeftCell = 0;//左边列数
            int sevenCount = 0;//列个数
            int sevenLastCell = 0;//最后三行列数
            int sevenCellCount = 0; //每行总列数
            int sevenMaxCellCount = 0; //列数最多的行数
            int eightLeftCell = 0;//每行最左像素位置
            int eightRightCell = 0;//每行最右像素位置
            int eightTopCount = 0;//上半部分圆圈区域
            int eightBottomCount = 0; //下半部分圆圈区域
            int eight0Count = 0; //空白区域长度
            int nineTopLeftCell = 0;//第一行最左
            int nineLastLeftCell = 0;//最后一行最左
            int nineLastRightCount = 0; //最后一列
            int nineLast0Count = 0;//最后一列空白间隔数
            foreach (var item in dic)
            {
                nowCount++;
                string key = item.Key.ToString();
                int value = item.Value;
                int nowRow = Convert.ToInt32(key.Length > 1 ? key[0].ToString() : "1");//当前像素所在行
                int nowCell = Convert.ToInt32(key.Length > 1 ? key[1].ToString() : key[0].ToString());//当前像素所在列
                //判断中心位置是否存在主体像素
                if (nowRow >= centre && nowRow <= centre + 2 && nowCell >= centre && nowCell <= centre + 2 && value == 1)
                    centreTrue = true;
                //判断是否为主体像素
                if (value == 1)
                    valTrue = true;
                else
                    valTrue = false;
                #region 数字 0
                //中间不会存在主体像素
                if (centreTrue)
                    numDic[0] = 1;// × 不符合0的特征
                //四边都存在主体像素
                else
                {
                    //记录当前行第一个和最后一个主体像素
                    if (nowCell > zeroRowOne && value == 1)
                    {
                        //当前行第一个主体像素
                        if (zeroRowLast == 0)
                            zeroRowLast = nowCell;
                        zeroRowOne = nowCell;//当前行最后一个主体像素
                    }

                    //只记录一行的数据,当前行最后一列时清空记录
                    if (nowCell % regionNumber == 0)
                    {
                        //如果第一个主体像素和最后一个主体像素相等说明当前行只有一个主体像素，存在缺口不是数字0的特征
                        if (zeroRowOne == zeroRowLast || zeroRowOne + 1 == zeroRowLast || zeroRowOne - 1 == zeroRowLast)
                        {
                            numDic[0] = 1;// × 不符合0的特征
                        }
                        //第一和最后的主体像素相距必须大于3,第一行和最有一行除外
                        else if (nowRow != 1 && nowRow != regionNumber && zeroRowOne - zeroRowLast <= 3)
                        {
                            numDic[0] = 1;// × 不符合0的特征
                        }
                        //清除当前行记录
                        zeroRowOne = 0;
                        zeroRowLast = 0;
                    }
                }
                #endregion

                #region 数字 1
                //判断图片比例
                if (width < 15 && (Convert.ToDouble(width) / Convert.ToDouble(height)) < 0.3)
                {
                    numDic[0] = 1;
                    numDic[1] = 0; // ✔ 确认为 1,停止判断其他数字特征并且全部设置为不符合
                    numDic[2] = 1;
                    numDic[3] = 1;
                    numDic[4] = 1;
                    numDic[5] = 1;
                    numDic[6] = 1;
                    numDic[7] = 1;
                    numDic[8] = 1;
                    numDic[9] = 1;
                    break;
                }
                //宽高超过一定比例则不是1
                else if ((Convert.ToDouble(width) / Convert.ToDouble(height)) > 0.6 || width > 50)
                {
                    numDic[1] = 1;// × 不符合1的特征                
                }
                #endregion

                #region 数字 2
                //记录中心位置下两行的主体像素
                if (centreTrue)
                {
                    //只记录左边的主体像素
                    if (nowRow == regionNumber / 2 + 3 && nowCell < regionNumber / 2 + 1 && valTrue)
                    {
                        twoLeftCell++;
                        if (nowCell > twoRightMax)
                            twoRightMax = nowCell;
                    }
                    //如果中心位置下两行右边没有主体像素则不是2
                    if (centreTrue && nowRow > regionNumber / 2 + 3 && twoLeftCell == 0 && twoRightMax > regionNumber / 2 + 2)
                        numDic[2] = 1;// × 不符合2的特征
                    if (nowRow == regionNumber && valTrue)
                        twoLastCell++;
                }
                else
                {
                    //如果中心不存在主体像素，判断倒数后两行最右边是否存在主体像素
                    if (nowRow == regionNumber - 1 || nowRow == regionNumber - 2)
                    {
                        //最右存在像素则不是2
                        if (nowCell == regionNumber && valTrue)
                            numDic[2] = 1;// × 不符合2的特征
                    }
                }

                //记录第一行最右边像素位置
                if (nowRow == 1 && valTrue)
                    twoTop1RightCell = nowCell;
                if (nowRow == 2 && valTrue)
                    twoTop2RightCell = nowCell;
                //第一行最右小于最右第二行则不是2
                if (nowRow == 3 && twoTop1RightCell > twoTop2RightCell)
                    numDic[2] = 1;// × 不符合2的特征
                //记录倒数第二行最右的像素位置
                if (nowRow == regionNumber - 1 && nowCell > twoRightLastMax1 && valTrue)//倒数第二行
                    twoRightLastMax1 = nowCell;
                //记录最后一行最右的像素位置
                if (nowRow == regionNumber && nowCell > twoRightLastMax2 && valTrue)//最后一行
                    twoRightLastMax2 = nowCell;
                //最后一行最右边的像素位置不能小于倒数第二最右边像素的位置
                if (nowRow == regionNumber && nowCell == regionNumber && twoRightLastMax1 > twoRightLastMax2)
                {
                    //  numDic[2] = 1;// × 不符合2的特征
                }
                //最后一行的前二行，判断最左边像素位置
                if (valTrue)
                {
                    if (nowRow == regionNumber - 1 && twoLeftMin1 == 0)//倒数第二行
                        twoLeftMin1 = nowCell;
                    if (nowRow == regionNumber - 2 && twoLeftMin2 == 0)//倒数第三行
                        twoLeftMin2 = nowCell;
                }
                //倒数第三行不能大于倒数第二行最左像素位置
                if (nowRow == regionNumber && nowCell == regionNumber && twoLeftMin2 < twoLeftMin1)
                    numDic[2] = 1;// × 不符合2的特征
                //记录第一和最后一行总列数
                if ((nowRow == 1 || nowRow == regionNumber) && valTrue)
                    twoTopLastCell++;
                //第一行最左位置和最后一行最右位置相加总列数不能小于一行的列数
                if (nowRow == regionNumber && nowCell == regionNumber && twoTopLastCell < regionNumber - 1)
                    numDic[2] = 1;// × 不符合2的特征

                #endregion

                #region 数字 3
                //记录倒数第二行最左边像素位置
                if (nowRow == regionNumber - 1 && valTrue && threeLastLeft1Cell == 0)
                    threeLastLeft1Cell = nowCell;
                //记录最后一行最左边的像素位置
                if (nowRow == regionNumber && threeLastLeftCell == 0 && valTrue)
                    threeLastLeftCell = nowCell;
                if (centreTrue)
                {
                    //记录中间最左边的像素位置
                    if ((threeCLeftCount == 0 || nowCell < threeCLeftCount) && valTrue)
                        threeCLeftCount = nowCell;
                    //中间最左边像素位置不能小于最后一行最左边像素
                    if (nowRow == regionNumber && nowCell == regionNumber && threeLastLeftCell < threeCLeftCount)
                        numDic[3] = 1; // × 不符合3的特征
                }
                //中间位置没有像素并且最后一行最左边像素大于或等于倒数第二行最左边位置则不是3
                else if (nowRow == regionNumber && !centreTrue && threeLastLeft1Cell <= threeLastLeftCell)
                {
                    numDic[3] = 1; // × 不符合3的特征
                }
                //记录第一行最左
                if (nowRow == 1 && valTrue)
                    threeTopCell++;
                //记录最后一行最右
                if (nowRow == regionNumber && valTrue)
                    threeLastCell++;
                //记录倒数第三行最右
                if (nowRow == regionNumber - 2 && valTrue)
                    threeLastRight2 = nowCell;
                //第一行最左位置和最后一行最右位置相加不能小于一行总列数,并且倒数第二行最右不能大于
                if (nowRow == regionNumber && nowCell == regionNumber)
                {
                    if (threeTopCell + threeLastCell < regionNumber)
                        numDic[3] = 1; // × 不符合3的特征
                    //为了避免和7混乱
                    if (numDic[7] == 0 && regionNumber - threeLastRight2 >= 2)
                        numDic[3] = 1; // × 不符合3的特征
                }
                //判断第三纵列圆圈间隔数
                if (nowCell == 3)
                {
                    if (valTrue)
                        threeLongitudinalTop = 1;
                    if (threeLongitudinalTop == 1 && !valTrue)
                        threeLongitudinal++;
                    if (threeLongitudinal > 0 && valTrue)
                    {
                        //如果间隔数小于4则不是3
                        if (threeLongitudinal <= regionNumber / 2)
                            numDic[3] = 1; // × 不符合3的特征
                        //如果间隔数大于4则不是数字 9
                        else
                            numDic[9] = 1; // × 不符合9的特征

                    }
                }
                #endregion

                #region 数字 4
                if (nowRow == regionNumber && valTrue)
                    fourLastCount++;
                if (fourLastCount >= regionNumber - 1)
                    numDic[4] = 1; // × 不符合4的特征

                #endregion

                #region 数字 5

                if (nowRow == 1 && valTrue)
                    fiveOneCell++;
                //判断前两行，第一行占比80%以上并且第二行右边存在主体像素则不是5
                //if (fiveOneCell >= regionNumber * 0.8)
                //    if (nowCell > regionNumber * 0.5 && valTrue)
                //        numDic[5] = 1;// × 不符合5的特征

                #endregion

                #region 数字 6
                //记录倒数第二行最左最右的像素位置
                if (nowRow == regionNumber - 1 && valTrue)
                {
                    if (sixLeftCell == 0)
                        sixLeftCell = nowCell;
                    sixRightCell = nowCell;
                }
                if (nowRow == regionNumber)
                {
                    if (sixRightCell - sixLeftCell <= 2)
                        numDic[6] = 1;// × 不符合6的特征
                }

                #endregion

                #region 数字 7
                //记录最后一行主体像素个数
                if (nowRow == regionNumber && valTrue)
                    sevenCount++;
                //如果最后一行主体像素存在比例大于一半则不是7
                if (sevenCount > regionNumber / 2)
                    numDic[7] = 1;// × 不符合7的特征
                //记录倒数第三行每行的列数
                if (nowRow > regionNumber - 3 && valTrue)
                {
                    sevenLastCell++;
                    if (nowCell % regionNumber == 0)
                    {
                        //其中一行有三列及以上则不是7
                        if (sevenLastCell >= 3)
                            numDic[7] = 1;// × 不符合7的特征
                        sevenLastCell = 0;
                    }
                }
                //记录每行总列数，判断每行的列数是否大于4，如果有超过两行总列数大于4则不是7
                if(valTrue)
                sevenCellCount++;
                if (sevenCellCount > regionNumber / 2)
                {
                    if (sevenMaxCellCount == 0)
                        sevenMaxCellCount = nowRow;
                    if (sevenMaxCellCount != 0 && sevenMaxCellCount != nowRow)
                        numDic[7] = 1;// × 不符合7的特征
                }
                if (nowCell == regionNumber)
                    sevenCellCount = 0;
                #endregion

                #region 数字 8
                //记录每行最左像素位置
                if (valTrue)
                    eightLeftCell = 1;
                //记录一行中0的个数
                if (eightLeftCell == 1 && !valTrue && eightRightCell == 0)
                    eight0Count++;
                if (eight0Count > 0 && valTrue)
                    eightRightCell = 1;
                if (nowCell == regionNumber)
                {
                    if (eightLeftCell == 1 && eightRightCell == 1 && eight0Count >= 3)
                    {
                        //记录上半部分圆圈行数
                        if (nowRow <= (regionNumber / 2) + 1)
                            eightTopCount++;
                        else
                            eightBottomCount++;
                    }
                    eight0Count = 0;
                    eightLeftCell = 0;
                    eightRightCell = 0;
                }

                if (nowRow == regionNumber && nowCell == regionNumber)
                {
                    if (eightTopCount == 0 || eightBottomCount == 0)
                        numDic[8] = 1;// × 不符合8的特征
                    else if (eightTopCount >= 1 && eightBottomCount >= 1)
                    {
                        numDic[1] = 1;
                        numDic[2] = 1;
                        numDic[3] = 1;
                        numDic[4] = 1;
                        numDic[5] = 1;
                        numDic[6] = 1;
                        numDic[7] = 1;
                        numDic[8] = 0;// ✔ 确认为 8,停止判断其他数字特征并且全部设置为不符合
                    }
                }
                #endregion

                #region 数字 9
                if (nowRow == 1 && nineTopLeftCell == 0)
                    nineTopLeftCell = nowCell;
                if (nowRow == regionNumber && nineLastLeftCell == 0)
                    nineLastLeftCell = nowCell;
                //如果中间没有像素，并且第一行最左比最后一行最左大则不是9
                if (nowRow == regionNumber && nowCell == regionNumber && !centreTrue && nineLastLeftCell < nineTopLeftCell)
                    numDic[9] = 1;

                if (nowCell == regionNumber)
                {
                    if (valTrue)
                        nineLastRightCount = 1;
                    if (nineLastRightCount == 1 && !valTrue)
                        nineLast0Count++;
                    //最后一列存在空白间隔则不是9
                    if(nineLast0Count>0&&valTrue)
                        numDic[9] = 1;
                }
                #endregion
            }

            return numDic;
        }


        /// <summary>
        /// 第三步：比对样本数据找到匹配的数字， 输出结果
        /// </summary>
        /// <param name="imgbase">图片Base64数据</param>
        /// <param name="number">对应的数字</param>
        /// <returns></returns>
        public string Result(string imgbase, string number = "")
        {
            JObject jobj = new JObject();
            imgbase = imgbase.Substring(imgbase.IndexOf(',') + 1);//省略base64开头部分的类型字符串
            byte[] b = Convert.FromBase64String(imgbase);
            MemoryStream ms = new MemoryStream(b);
            Bitmap bitObj = new Bitmap(ms);
            bitObj = ZoomImage(bitObj, 164, 164);//压缩图片，优化识别速度
            List<List<object>> pixelList = MainPixel(bitObj);//获取主体像素数据集合
            Bitmap bitmap = new Bitmap(width, height);
            int avgX = 0;//x轴平均值
            int avgY = 0;//y轴平均值
            int rX = 1;//当前x轴区域
            int rY = 1;//当前y轴区域
            string pinfo = "";
            int nowcount = 0;
            int count = 0;
            avgX = (int)width / regionNumber;//x轴区域平均值
            avgY = (int)height / regionNumber;//y轴区域平均值
            avgX = avgX == 0 ? 1 : avgX;
            avgY = avgY == 0 ? 1 : avgY;
            //先生成空白的数据模型
            Dictionary<string, int> dic = new Dictionary<string, int>();
            for (int r1 = 1; r1 <= regionNumber; r1++)
            {
                for (int r2 = 1; r2 <= regionNumber; r2++)
                {
                    dic[r1.ToString() + r2.ToString()] = 0;
                }
            }
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    List<object> item = pixelList[nowcount];
                    int x = Convert.ToInt32(item[0]) - startX;//x坐标
                    int y = Convert.ToInt32(item[1]) - startY;//y坐标
                    //计算当前像素所处区域
                    for (int r = 0; r < regionNumber; r++)
                    {
                        if (x >= r * avgX)
                        {
                            rX = r + 1;
                        }
                        if (y >= r * avgY)
                        {
                            rY = r + 1;
                        }
                        #region 画辅助线
                        //if (r > 0 && ((i == avgX * r) || (j == avgY * r)))
                        //{
                        //    bitmap.SetPixel(i, j, Color.FromArgb(255, Color.Green));//画线                       
                        //}
                        #endregion
                    }
                    //主体像素部分
                    if (x == i && y == j)
                    {
                        if (nowcount < pixelList.Count - 1)
                            nowcount++;
                        Color c = (dynamic)item[2];//颜色
                        pinfo += "1";
                        bitmap.SetPixel(x, y, c);//处理后的图片
                        count++;
                        dic[rY.ToString() + rX.ToString()] = 1;//表示当前像素是识别对象
                    }
                    else
                    {
                        pinfo += "0";
                    }
                }
                pinfo += "<br/>";
            }
            jobj["w"] = width;
            jobj["h"] = height;
            jobj["info"] = pinfo;

            string dicstr = "";
            string infostr = "";//新数据
            var list = from d in dic orderby d.Key select d;//排序
            int num = 0;
            foreach (var item in list)
            {
                infostr += item.Value.ToString();
                num++;
                dicstr += "<span style='margin-right:3px;height:20px;" + (item.Value == 0 ? "color:#808080;" : "") + "'>" + item.Value + "</span>";
                if (num % regionNumber == 0)
                    dicstr += "<br/>";
            }
            jobj["str"] = dicstr;
            int trueNumber = -1;
            JObject infoJobj = new JObject();
            string txtfile = System.Web.HttpContext.Current.Server.MapPath("/Data/info.txt");
            StreamReader sr = new StreamReader(txtfile);
            string resstr = sr.ReadLine(); //读取每行数据
            sr.Close();

            string falseStr = "";
            JObject falseJobj = new JObject();
            Dictionary<int, int> numDic = FeaturesCheck(dic);//过滤不符合特征的数字
            foreach (var item in numDic)
            {
                if (item.Value == 2)
                    trueNumber = Convert.ToInt32(item.Key);
            }
            //对比样本数据
            if (resstr != null)
            {
                string[] strlist = resstr.Split(',');//分割成数组
                for (int i = 0; i < strlist.Length - 1; i++)
                {
                    int infoCount = 0;
                    int nownum = Convert.ToInt32(strlist[i].Substring(0, 1));
                    string liststr = strlist[i].Substring(strlist[i].IndexOf("#") + 1);
                    for (int s = 0; s < liststr.Length; s++)
                    {
                        bool isSuccess = false;
                        if (infostr[s] == liststr[s])// && liststr[s] == '1'
                        {
                            isSuccess = true;
                            //验证是否符合特征
                            foreach (var item in numDic)
                            {
                                //过滤不符合特征的数字，不进行匹配
                                if (nownum == item.Key && item.Value == 1)
                                {
                                    isSuccess = false;
                                    falseJobj[item.Key.ToString()] = "";
                                    if (!falseStr.Contains(item.Key.ToString()))
                                        falseStr += item.Key.ToString() + "，";
                                    infoJobj[strlist[i].Substring(0, 1)] = "-";
                                }
                            }
                        }
                        if (isSuccess)
                            infoCount++;
                    }
                    decimal bfb = Convert.ToDecimal(infoCount.ToString()) / Convert.ToDecimal((regionNumber * regionNumber).ToString()) * 100;//计算匹配度（百分比）
                    infoJobj[strlist[i].Substring(0, 1)] = bfb.ToString("0.00") + "%";
                }
            }
            jobj["data"] = infoJobj.ToString();
            decimal maxbfb = 0;
            decimal top2bfb = 0;
            decimal top3bfb = 0;
            string maxId = "";
            string top2Id = "";
            string top3Id = "";
            foreach (var item in infoJobj)
            {
                decimal nowbfb = Convert.ToDecimal(item.Value.ToString().Replace("%", ""));
                if (nowbfb == 0) continue;
                if (item.Value.ToString() != "-")
                {
                    if (nowbfb > maxbfb)
                    {
                        top2bfb = maxbfb;
                        top2Id = maxId;
                        maxbfb = nowbfb;
                        maxId = item.Key;
                    }
                    if (nowbfb > top2bfb && nowbfb < maxbfb)
                    {
                        top3bfb = top2bfb;
                        top3Id = top2Id;
                        top2bfb = nowbfb;
                        top2Id = item.Key;
                    }
                    if (nowbfb > top3bfb && nowbfb < top2bfb)
                    {
                        top3bfb = nowbfb;
                        top3Id = item.Key;
                    }
                }
            }
            jobj["info"] = "<span style='font-size:20px;'><b>" + maxId + "</b></span>" + "<br/><br/>样本匹配度：<br/>" + maxbfb + "%  ———  " + maxId + "<br/>" + top2bfb + "%  ———  " + top2Id + "<br/>" + top3bfb + "%  ———  " + top3Id + "<br/><br/>不匹配的数字：" + (falseStr.Length > 0 ? falseStr.Substring(0, falseStr.Length - 1) : falseStr) + "<br/><br/> ";
            jobj["number"] = maxId;
            if (string.IsNullOrEmpty(number))
                number = maxId;
            string nowDtstr = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            //保存图片
            string imgpath = System.Web.HttpContext.Current.Server.MapPath("/Data/" + nowDtstr + ".png");
            bitmap.Save(imgpath, ImageFormat.Png);
            //保存像素模型数据
            StreamWriter sw = new StreamWriter(System.Web.HttpContext.Current.Server.MapPath("/Data/new_info.txt"), true);
            sw.Write(number + "-" + nowDtstr + "#" + infostr + ",");
            sw.Close();
            jobj["numberimg"] = Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "") + "/Data/" + nowDtstr + ".png";
            return jobj.ToString();
        }

        /// <summary>
        /// 等比例缩放图片
        /// </summary>
        /// <param name="bitmap">图片</param>
        /// <param name="destHeight">压缩后高度</param>
        /// <param name="destWidth">压缩后宽度</param>
        /// <returns></returns>
        private Bitmap ZoomImage(Bitmap bitmap, int destHeight, int destWidth)
        {
            try
            {
                System.Drawing.Image sourImage = bitmap;
                int width = 0, height = 0;
                //按比例缩放           
                int sourWidth = sourImage.Width;
                int sourHeight = sourImage.Height;
                if (sourHeight > destHeight || sourWidth > destWidth)
                {
                    if ((sourWidth * destHeight) > (sourHeight * destWidth))
                    {
                        width = destWidth;
                        height = (destWidth * sourHeight) / sourWidth;
                    }
                    else
                    {
                        height = destHeight;
                        width = (sourWidth * destHeight) / sourHeight;
                    }
                }
                else
                {
                    width = sourWidth;
                    height = sourHeight;
                }
                Bitmap destBitmap = new Bitmap(destWidth, destHeight);
                Graphics g = Graphics.FromImage(destBitmap);
                g.Clear(Color.Transparent);
                //设置画布的描绘质量         
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(sourImage, new Rectangle((destWidth - width) / 2, (destHeight - height) / 2, width, height), 0, 0, sourImage.Width, sourImage.Height, GraphicsUnit.Pixel);
                g.Dispose();
                //设置压缩质量     
                System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters();
                long[] quality = new long[1];
                quality[0] = 100;
                System.Drawing.Imaging.EncoderParameter encoderParam = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                encoderParams.Param[0] = encoderParam;
                sourImage.Dispose();
                return destBitmap;
            }
            catch
            {
                return bitmap;
            }
        }
    }
}
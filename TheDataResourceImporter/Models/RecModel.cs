using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TheDataResourceImporter.Models
{
    public class RecModel
    {
        /// <summary>
        /// 申请号
        /// </summary>
        public string AN { get; set; }

        /// <summary>
        /// 公布日期
        /// </summary>
        public string SWPUBDATE { get; set; }

        /// <summary>
        /// 法律状态
        /// </summary>
        public string FLZT { get; set; }

        /// <summary>
        /// 法律状态信息前缀
        /// </summary>
        public string FLZTInfoBefore { get; set; } 

        /// <summary>
        /// 法律状态信息
        /// </summary>
        public string FLZTInfo { get; set; } 
    }
}

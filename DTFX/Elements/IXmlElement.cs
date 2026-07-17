using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    public interface IXmlElement
    {
        /// <summary>
        /// 要素のID
        /// </summary>
        string Id { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NATS_WorkQueue.SharedCode
{
    public class RequestModel
    {
        public ulong ID { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}

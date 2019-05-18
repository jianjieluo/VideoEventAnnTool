using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace event_ann.Models
{
    class EventItem
    {
        private string id;

        public string Id
        {
            get { return id; }
            private set { id = value; }
        }

        public double st_time { get; set; }
        public double ed_time { get; set; }
        public string caption { get; set; }
        public string AnnShowString
        {
            get
            {
                var st_str = String.Format("{0:0.00}", this.st_time);
                var ed_str = String.Format("{0:0.00}", this.ed_time);
                return (st_str + "  -  " + ed_str + " " + this.caption).Trim();
            }
        }
        public string AnnRecordString
        {
            get
            {
                var st_str = String.Format("{0:0.00}", this.st_time);
                var ed_str = String.Format("{0:0.00}", this.ed_time);
                return (st_str + " " + ed_str + " " + this.caption).Trim();
            }
        }

        public EventItem(double st, double ed, string caption = "")
        {
            this.id = Guid.NewGuid().ToString(); //生成id
            this.st_time = st;
            this.ed_time = ed;
            this.caption = caption;
        }
    }
}

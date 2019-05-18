using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace event_ann
{
    // This is a simple customer class that   
    // implements the IPropertyChange interface.  
    public class DataBinding : INotifyPropertyChanged
    {
        // These fields hold the values for the public properties.  
        private double start = 0;
        private double end = 0;
        private string start_str = String.Empty;
        private string end_str = String.Empty;
        private string process = String.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // The constructor is private to enforce the factory pattern.  
        private DataBinding()
        {
            start = 0;
            end = 0;
            start_str = String.Empty;
            end_str = String.Empty;
            process = String.Empty;
        }

        // This is the public factory method.  
        public static DataBinding CreateNewDataBinding()
        {
            return new DataBinding();
        }

        // This property represents an ID, suitable  
        // for use as a primary key in a database.  
        public double Start
        {
            get
            {
                return this.start;
            }

            set
            {
                if (value != this.start)
                {
                    this.start = value;
                    NotifyPropertyChanged();
                    this.Start_str = String.Format("{0:0.00}", this.start);
                }
            }
        }

        public double End
        {
            get
            {
                return this.end;
            }

            set
            {
                if (value != this.end)
                {
                    this.end = value;
                    NotifyPropertyChanged();
                    this.End_str = String.Format("{0:0.00}", this.end);
                }
            }
        }

        public string Start_str
        {
            get
            {
                return this.start_str;
            }
            set
            {
                if (value != this.start_str)
                {
                    this.start_str = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string End_str
        {
            get
            {
                return this.end_str;
            }
            set
            {
                if (value != this.end_str)
                {
                    this.end_str = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Process
        {
            get
            {
                return this.process;
            }

            set
            {
                if (value != this.process)
                {
                    this.process = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}

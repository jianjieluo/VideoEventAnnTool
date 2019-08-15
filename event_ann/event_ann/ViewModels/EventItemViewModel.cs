using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using event_ann.Common;

namespace event_ann.ViewModels
{
    class EventItemViewModel : BindableBase
    {
        private ObservableCollection<Models.EventItem> allItems = new ObservableCollection<Models.EventItem>();
        public ObservableCollection<Models.EventItem> AllItems
        {
            get { return this.allItems; }
            set
            {
                SetProperty(ref allItems, value);
            }
        }

        private Models.EventItem selectedItem = default(Models.EventItem);
        public Models.EventItem SelectedItem { get { return selectedItem; } set { this.selectedItem = value; } }

        private string video_path;
        private StorageFolder outputFolder;
        private string annFileName;
        public string AnnFilePath
        {
            get
            {
                return Path.Combine(this.outputFolder.Path, this.annFileName);
            }
        }
        public bool IsAnnEmpty
        {
            get
            {
                return !allItems.Any();
            }
        }

        public EventItemViewModel(string video_path, StorageFolder _outputFolder)
        {
            Initialization(video_path, _outputFolder);
        }

        public EventItemViewModel()
        {
        }

        public void Initialization(string video_path, StorageFolder _outputFolder)
        {
            this.video_path = video_path;
            this.outputFolder = _outputFolder;
            this.annFileName = Path.GetFileNameWithoutExtension(this.video_path) + @".txt";

            this.allItems.Clear();
            if (File.Exists(AnnFilePath))
            {
                LoadExistedAnn();
            }

            // Mock Data for Debug
            //AddMockData();
        }

        private void AddMockData()
        {
            for (int tmp = 2; tmp < 10; ++tmp)
            {
                this.allItems.Add(
                    new Models.EventItem(0, tmp, "aaa")
                );
            }
        }

        private async void LoadExistedAnn()
        {
            var file = await this.outputFolder.GetFileAsync(this.annFileName);
            var readFile = await FileIO.ReadLinesAsync(file);
            foreach (var line in readFile)
            {
                string[] sp = line.Split(' ');
                double st = Convert.ToDouble(sp[0]);
                double ed = Convert.ToDouble(sp[1]);
                string caption = (sp.Count() == 3) ? sp[2] : "";

                this.allItems.Add(
                    new Models.EventItem(st, ed, caption)
                );
            }
        }

        /// <summary>
        /// 增删
        /// </summary>
        public void AddEventItem(double st, double ed, string caption = "")
        {
            if (IsLabelValid(st, ed))
            {
                this.allItems.Add(new Models.EventItem(st, ed, caption));
            }
        }

        public void RemoveEventItem(string id)
        {
            allItems.Remove(FindEventItem(id));
            // set selectedItem to null after remove
            this.selectedItem = null;
        }

        public Models.EventItem FindEventItem(string id)
        {
            foreach (var item in this.allItems)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }
            return default(Models.EventItem);
        }

        private bool IsLabelValid(double st, double ed)
        {
            return (st >= 0 && ed >= 0 && st <= ed);
        }

        public async Task SaveAnnAsync()
        {
            if (!IsAnnEmpty)
            {
                string[] lines = new string[allItems.Count];
                int iter = 0;
                foreach (var item in allItems)
                {
                    lines[iter] = item.AnnRecordString;
                    ++iter;
                }
                await this.outputFolder.CreateFileAsync(this.annFileName,
                    Windows.Storage.CreationCollisionOption.ReplaceExisting);
                StorageFile file = await this.outputFolder.GetFileAsync(this.annFileName);
                await FileIO.WriteLinesAsync(file, lines);
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.System;
using event_ann.Common;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace event_ann
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// 固定数据
        /// </summary>
        const string ALLDONE = "All Done";

        /// <summary>
        /// 数据成员变量
        /// </summary>
        private string[] video_list;
        ViewModels.EventItemViewModel ViewModel { get; set; }
        DataBinding ViewInfo { get; set; }
        private StorageFolder outputFolder;
        private StorageFolder videosFolder;
        private StorageFolder LocalFolder
        {
            get
            {
                return Windows.Storage.ApplicationData.Current.LocalFolder;
            }
        }
        public string ProcessString
        {
            get
            {
                return $"{this.db_cursor + 1}" + "/" + $"{this.video_num}";
            }
        }

        /// <summary>
        /// 状态成员变量
        /// </summary>
        private int video_num;
        private int db_cursor;
        private bool isInited;

        private bool isNextIndexOutRange
        {
            get
            {
                return (db_cursor + 1) >= this.video_num;
            }
        }
        private bool isPreviousIndexOutRange
        {
            get
            {
                return (db_cursor - 1) < 0;
            }
        }
        private bool hasCurrCursorAnned
        {
            get
            {
                string filename = Path.GetFileNameWithoutExtension(video_list[db_cursor]) + @".txt";
                string ann_file_path = Path.Combine(outputFolder.Path, filename);
                return File.Exists(ann_file_path);
            }
        }

        /// <summary>
        /// 程序加载成员函数
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.ViewModel = new ViewModels.EventItemViewModel();
            this.ViewInfo = DataBinding.CreateNewDataBinding();
            // 快捷键支持
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
            isInited = false;
        }

        private async Task InitializeAppAsync()
        {
            await LoadVideoList();
            await SelectVediosFolder();
            outputFolder = LocalFolder;

            this.db_cursor = -1;
            string video_path = GetNextVideoPath(true);

            if (video_path == ALLDONE)
            {
                video_path = video_list[0];
                ShowMessageDialog(2);
            }

            this.ViewModel.Initialization(video_path, outputFolder);
            await LoadMediaFromPath(video_path);

            ViewInfo.Process = ProcessString;
            ViewInfo.Start = 0;
            ViewInfo.End = 0;
        }

        private async Task SelectVediosFolder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add(".mp4");
            folderPicker.FileTypeFilter.Add(".wmv");
            videosFolder = await folderPicker.PickSingleFolderAsync();
            if (videosFolder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", videosFolder);
            }
        }

        private async Task LoadVideoList()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".txt");

            var file = await picker.PickSingleFileAsync();
            var readFile = await Windows.Storage.FileIO.ReadLinesAsync(file);
            this.video_num = readFile.Count();
            // Construct video list
            this.video_list = new string[this.video_num];
            int iter = 0;
            foreach (string line in readFile)
            {
                this.video_list[iter] = line;
                ++iter;
            }
        }

        private void CursorNextStep()
        {
            db_cursor = (isNextIndexOutRange) ? 0 : (db_cursor + 1);
        }

        private void CursorPreviousStep()
        {
            db_cursor = (isPreviousIndexOutRange) ? (video_num - 1) : (db_cursor - 1);
        }

        private string GetNextVideoPath(bool isForceUnanned = false)
        {
            CursorNextStep();

            int start_cursor = db_cursor;
            while (isForceUnanned && hasCurrCursorAnned)
            {
                CursorNextStep();
                if (start_cursor == db_cursor)
                {
                    db_cursor = 0;
                    return ALLDONE;
                }
            }

            return this.video_list[db_cursor];
        }

        private string GetPreviousVideoPath(bool isForceUnanned = false)
        {
            CursorPreviousStep();

            int start_cursor = db_cursor;
            while (isForceUnanned && hasCurrCursorAnned)
            {
                CursorPreviousStep();
                if (start_cursor == db_cursor)
                {
                    db_cursor = 0;
                    return ALLDONE;
                }
            }

            return this.video_list[db_cursor];
        }

        private async Task LoadMediaFromPath(string name)
        {
            var file = await videosFolder.GetFileAsync(name);
            if (file != null)
            {
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                mediaPlayer.SetSource(stream, file.ContentType);
            }
        }

        private double GetCurrVideoSecond()
        {
            var value = mediaPlayer.Position;
            return (value == null) ? 0 : ((TimeSpan)value).TotalSeconds;
        }

        private async void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (isInited)
            {
                if (args.EventType.ToString().Contains("Down"))
                {
                    var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                    if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        switch (args.VirtualKey)
                        {
                            case VirtualKey.S:
                                ViewInfo.Start = GetCurrVideoSecond();
                                break;
                            case VirtualKey.E:
                                ViewInfo.End = GetCurrVideoSecond();
                                break;
                            case VirtualKey.D:
                                ViewModel.AddEventItem(ViewInfo.Start, ViewInfo.End);
                                break;
                            case VirtualKey.F:
                                if (ViewModel.IsAnnEmpty)
                                {
                                    ShowMessageDialog(0);
                                    return;
                                }
                                await ViewModel.SaveAnnAsync();
                                string video_path = GetNextVideoPath();
                                await LoadViewModelFromPath(video_path);
                                break;
                        }
                    }
                }
            }
        }

        private void OnClickStart(object sender, RoutedEventArgs e)
        {
            if (isInited)
                ViewInfo.Start = GetCurrVideoSecond();
        }

        private void OnClickEnd(object sender, RoutedEventArgs e)
        {
            if (isInited)
                ViewInfo.End = GetCurrVideoSecond();
        }

        private void OnClickConfirm(object sender, RoutedEventArgs e)
        {
            if (isInited)
                ViewModel.AddEventItem(ViewInfo.Start, ViewInfo.End);
        }

        private void OnClickDelete(object sender, RoutedEventArgs e)
        {
            if (isInited && ViewModel.SelectedItem != null)
            {
                ViewModel.RemoveEventItem(ViewModel.SelectedItem.Id);
            }
        }

        private async Task LoadViewModelFromPath(string video_path)
        {
            await LoadMediaFromPath(video_path);
            ViewModel.Initialization(video_path, outputFolder);
            ViewInfo.Process = ProcessString;
            ViewInfo.Start = 0;
            ViewInfo.End = 0;
        }

        private async void OnClickPrevious(object sender, RoutedEventArgs e)
        {
            if (isInited)
            {
                if (ViewModel.IsAnnEmpty)
                {
                    ShowMessageDialog(0);
                    return;
                }
                await ViewModel.SaveAnnAsync();
                string video_path = GetPreviousVideoPath();
                await LoadViewModelFromPath(video_path);
            }
        }

        private async void OnClickNext(object sender, RoutedEventArgs e)
        {
            if (isInited)
            {
                if (ViewModel.IsAnnEmpty)
                {
                    ShowMessageDialog(0);
                    return;
                }
                await ViewModel.SaveAnnAsync();
                string video_path = GetNextVideoPath();
                await LoadViewModelFromPath(video_path);
            }
        }

        private void EventItem_Clicked(object sender, ItemClickEventArgs e)
        {
            if (isInited)
            {
                ViewModel.SelectedItem = (Models.EventItem)(e.ClickedItem);
            }
        }

        private async void ShowMessageDialog(int type)
        {
            string message = String.Empty;
            switch (type)
            {
                case 0:
                    message = "这个视频还没任何标注。";
                    break;
                case 1:
                    message = "Pick Output folder error.";
                    break;
                case 2:
                    message = "全部标注完成。";
                    break;
                case 3:
                    message = "导出完成。";
                    break;
                default:
                    message = "Something need to be informed.";
                    break;
            }

            var msgDialog = new Windows.UI.Popups.MessageDialog(message) { Title = "提示" };
            await msgDialog.ShowAsync();
        }

        private void OnClickShowLocalFolder(object sender, RoutedEventArgs e)
        {
            ViewInfo.Process = LocalFolder.Path;
        }

        /// <summary>
        /// Choose an export Folder, export the ann file in LocalFolder to the selected folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnClickExport2Foloder(object sender, RoutedEventArgs e)
        {
            if (isInited)
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add(".txt");
                var exportFolder = await folderPicker.PickSingleFolderAsync();
                if (exportFolder != null)
                {
                    // Application now has read/write access to all contents in the picked folder
                    // (including other sub-folder contents)
                    Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace("PickedFolderToken", exportFolder);

                    await Export2Folder(exportFolder);
                    ShowMessageDialog(3);
                }
            }
        }

        private async Task Export2Folder(StorageFolder exportFolder)
        {
            foreach (var file in await LocalFolder.GetFilesAsync())
            {
                await file.CopyAsync(exportFolder, file.Name, NameCollisionOption.ReplaceExisting);
            }
        }

        private async void OnClickSaveCurrAnn(object sender, RoutedEventArgs e)
        {
            if (isInited)
            {
                if (ViewModel.IsAnnEmpty)
                {
                    ShowMessageDialog(0);
                    return;
                }
                await ViewModel.SaveAnnAsync();
            }
        }

        private async void OnClickInitilze(object sender, RoutedEventArgs e)
        {
            if (!isInited)
            {
                await InitializeAppAsync();
                isInited = true;
            }
        }
    }
}

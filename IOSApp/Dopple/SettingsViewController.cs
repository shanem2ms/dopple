using Foundation;
using System;
using UIKit;
using System.IO;

namespace Dopple
{
    public partial class SettingsViewController : UIViewController
    {
        public static string hostServerStr = "hostserver";
        UDPer.UDPer udp = new UDPer.UDPer();

        public SettingsViewController (IntPtr handle) : base (handle)
        {
        }

        string[] recordingFiles;
        int selectedRecordingIdx = -1;
        public override void ViewDidLoad()
        {
            var plist = NSUserDefaults.StandardUserDefaults;
            string hostStr = plist.StringForKey(hostServerStr);
            if (hostStr != null)
                this.hostServer.Text = hostStr;
            base.ViewDidLoad();
            //udp.Start();
            RefreshRecordings();
            this.recordingsView.Delegate = new RecordingsDelegate(this);

        }

        void RefreshRecordings()
        {
            var documents =
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            this.recordingFiles = Directory.GetFiles(documents);
            this.recordingsView.Source = new RecordingTblSource(recordingFiles);
            this.recordingsView.ReloadData();
        }

        partial void OnSendFileDown(UIButton sender)
        {
            if (selectedRecordingIdx >= 0)
            {
                DataTransmit dt = new DataTransmit();
                dt.TransferRecording(this.recordingFiles[this.selectedRecordingIdx]);
            }
        }

        partial void OnDelFileDown(UIButton sender)
        {
            if (selectedRecordingIdx >= 0)
            {
                File.Delete(this.recordingFiles[this.selectedRecordingIdx]);
                RefreshRecordings();
            }
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            this.View.EndEditing(true);
            var plist = NSUserDefaults.StandardUserDefaults;
            plist.SetString(this.hostServer.Text, hostServerStr);
            plist.Synchronize();
            base.TouchesBegan(touches, evt);
        }

        void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            this.selectedRecordingIdx = indexPath.Row;
            FileInfo fi = new FileInfo(this.recordingFiles[selectedRecordingIdx]);
            long lenMB = fi.Length / 1000000L;
            this.sendFileLbl.Text = $"{fi.Name} : {lenMB}mb";

        }

        public class RecordingTblSource : UITableViewSource
        {

            string[] FilePaths;
            string CellIdentifier = "TableCell";

            public RecordingTblSource(string[] items)
            {
                FilePaths = items;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return FilePaths.Length;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
                string item = FilePaths[indexPath.Row];
                string filename = System.IO.Path.GetFileName(item);

                //---- if there are no cells to reuse, create a new one
                if (cell == null)
                { cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier); }

                cell.TextLabel.Text = filename;

                return cell;
            }
        }

        class RecordingsDelegate : UITableViewDelegate
        {
            SettingsViewController pthis;

            public RecordingsDelegate(SettingsViewController pt)
            { pthis = pt; }
            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                pthis.RowSelected(tableView, indexPath);
            }
        }
    }


}
// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace Dopple
{
    [Register ("SettingsViewController")]
    partial class SettingsViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton delFileBtn { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField hostServer { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView recordingsView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton sendFileBtn { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel sendFileLbl { get; set; }

        [Action ("OnDelFileDown:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnDelFileDown (UIKit.UIButton sender);

        [Action ("OnSendFileDown:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnSendFileDown (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (delFileBtn != null) {
                delFileBtn.Dispose ();
                delFileBtn = null;
            }

            if (hostServer != null) {
                hostServer.Dispose ();
                hostServer = null;
            }

            if (recordingsView != null) {
                recordingsView.Dispose ();
                recordingsView = null;
            }

            if (sendFileBtn != null) {
                sendFileBtn.Dispose ();
                sendFileBtn = null;
            }

            if (sendFileLbl != null) {
                sendFileLbl.Dispose ();
                sendFileLbl = null;
            }
        }
    }
}
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
    [Register ("GLViewController")]
    partial class GLViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton backBtn { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel depthLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton liveBtn { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton recordBtn { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton vidSclBtn { get; set; }

        [Action ("OnLiveBtnDown:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnLiveBtnDown (UIKit.UIButton sender);

        [Action ("OnRecordBtnDown:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnRecordBtnDown (UIKit.UIButton sender);

        [Action ("OnVidDownSmpDown:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnVidDownSmpDown (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (backBtn != null) {
                backBtn.Dispose ();
                backBtn = null;
            }

            if (depthLabel != null) {
                depthLabel.Dispose ();
                depthLabel = null;
            }

            if (liveBtn != null) {
                liveBtn.Dispose ();
                liveBtn = null;
            }

            if (recordBtn != null) {
                recordBtn.Dispose ();
                recordBtn = null;
            }

            if (vidSclBtn != null) {
                vidSclBtn.Dispose ();
                vidSclBtn = null;
            }
        }
    }
}
namespace LibreHardwareService
{
    partial class LibreHardwareService
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.eventLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
            // 
            // eventLog
            // 
            this.eventLog.Log = "Application";
            this.eventLog.EntryWritten += new System.Diagnostics.EntryWrittenEventHandler(this.EventLog_EntryWritten);
            // 
            // LibreHardwareService
            // 
            this.ServiceName = "LibreHardwareService";
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();

        }

		#endregion

		private System.Diagnostics.EventLog eventLog;
	}
}

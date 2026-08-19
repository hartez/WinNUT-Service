namespace WinNUT.SimulatorGUI
{
	partial class UPSSimulator
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			Charge = new NumericUpDown();
			UpdateButton = new Button();
			OnBattery = new RadioButton();
			Online = new RadioButton();
			Status = new GroupBox();
			ChargeLabel = new Label();
			BatteryRemaining = new NumericUpDown();
			label1 = new Label();
			SerialNumber = new TextBox();
			SerialNumberLabel = new Label();
			((System.ComponentModel.ISupportInitialize)Charge).BeginInit();
			Status.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)BatteryRemaining).BeginInit();
			SuspendLayout();
			// 
			// Charge
			// 
			Charge.Location = new Point(38, 50);
			Charge.Name = "Charge";
			Charge.Size = new Size(120, 23);
			Charge.TabIndex = 0;
			// 
			// UpdateButton
			// 
			UpdateButton.Location = new Point(185, 237);
			UpdateButton.Name = "UpdateButton";
			UpdateButton.Size = new Size(75, 23);
			UpdateButton.TabIndex = 1;
			UpdateButton.Text = "Update";
			UpdateButton.UseVisualStyleBackColor = true;
			UpdateButton.Click += Update_Click;
			// 
			// OnBattery
			// 
			OnBattery.AutoSize = true;
			OnBattery.Location = new Point(25, 21);
			OnBattery.Name = "OnBattery";
			OnBattery.Size = new Size(81, 19);
			OnBattery.TabIndex = 2;
			OnBattery.TabStop = true;
			OnBattery.Text = "On Battery";
			OnBattery.UseVisualStyleBackColor = true;
			// 
			// Online
			// 
			Online.AutoSize = true;
			Online.Location = new Point(25, 62);
			Online.Name = "Online";
			Online.Size = new Size(76, 19);
			Online.TabIndex = 3;
			Online.TabStop = true;
			Online.Text = "On Mains";
			Online.UseVisualStyleBackColor = true;
			// 
			// Status
			// 
			Status.Controls.Add(Online);
			Status.Controls.Add(OnBattery);
			Status.Location = new Point(249, 29);
			Status.Name = "Status";
			Status.Size = new Size(200, 100);
			Status.TabIndex = 4;
			Status.TabStop = false;
			Status.Text = "Status";
			// 
			// ChargeLabel
			// 
			ChargeLabel.AutoSize = true;
			ChargeLabel.Location = new Point(38, 29);
			ChargeLabel.Name = "ChargeLabel";
			ChargeLabel.Size = new Size(45, 15);
			ChargeLabel.TabIndex = 5;
			ChargeLabel.Text = "Charge";
			// 
			// BatteryRemaining
			// 
			BatteryRemaining.Location = new Point(38, 113);
			BatteryRemaining.Name = "BatteryRemaining";
			BatteryRemaining.Size = new Size(120, 23);
			BatteryRemaining.TabIndex = 6;
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(38, 95);
			label1.Name = "label1";
			label1.Size = new Size(150, 15);
			label1.TabIndex = 7;
			label1.Text = "Battery Minutes Remaining";
			// 
			// SerialNumber
			// 
			SerialNumber.Location = new Point(38, 182);
			SerialNumber.Name = "SerialNumber";
			SerialNumber.Size = new Size(120, 23);
			SerialNumber.TabIndex = 8;
			// 
			// SerialNumberLabel
			// 
			SerialNumberLabel.AutoSize = true;
			SerialNumberLabel.Location = new Point(38, 164);
			SerialNumberLabel.Name = "SerialNumberLabel";
			SerialNumberLabel.Size = new Size(82, 15);
			SerialNumberLabel.TabIndex = 9;
			SerialNumberLabel.Text = "Serial Number";
			// 
			// UPSSimulator
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(469, 302);
			Controls.Add(SerialNumberLabel);
			Controls.Add(SerialNumber);
			Controls.Add(label1);
			Controls.Add(BatteryRemaining);
			Controls.Add(ChargeLabel);
			Controls.Add(Status);
			Controls.Add(UpdateButton);
			Controls.Add(Charge);
			Name = "UPSSimulator";
			Text = "UPS Simulator";
			Load += UPSSimulator_Load;
			((System.ComponentModel.ISupportInitialize)Charge).EndInit();
			Status.ResumeLayout(false);
			Status.PerformLayout();
			((System.ComponentModel.ISupportInitialize)BatteryRemaining).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private NumericUpDown Charge;
		private Button UpdateButton;
		private RadioButton OnBattery;
		private RadioButton Online;
		private GroupBox Status;
		private Label ChargeLabel;
		private NumericUpDown BatteryRemaining;
		private Label label1;
		private TextBox SerialNumber;
		private Label SerialNumberLabel;
	}
}

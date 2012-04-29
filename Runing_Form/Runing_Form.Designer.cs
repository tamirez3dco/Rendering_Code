namespace Runing_Form
{
    partial class Runing_Form
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.numOfInstances_textBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.restartRhinosButton = new System.Windows.Forms.Button();
            this.check_ShutDown_Condition_Timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // numOfInstances_textBox
            // 
            this.numOfInstances_textBox.BackColor = System.Drawing.Color.Red;
            this.numOfInstances_textBox.Enabled = false;
            this.numOfInstances_textBox.Location = new System.Drawing.Point(132, 24);
            this.numOfInstances_textBox.Name = "numOfInstances_textBox";
            this.numOfInstances_textBox.Size = new System.Drawing.Size(27, 20);
            this.numOfInstances_textBox.TabIndex = 5;
            this.numOfInstances_textBox.Text = "2";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(114, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "How many instances ?";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(15, 113);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(110, 39);
            this.button1.TabIndex = 7;
            this.button1.Text = "flipVisibility";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // restartRhinosButton
            // 
            this.restartRhinosButton.Location = new System.Drawing.Point(23, 179);
            this.restartRhinosButton.Name = "restartRhinosButton";
            this.restartRhinosButton.Size = new System.Drawing.Size(101, 37);
            this.restartRhinosButton.TabIndex = 8;
            this.restartRhinosButton.Text = "Restart Rhinos";
            this.restartRhinosButton.UseVisualStyleBackColor = true;
            this.restartRhinosButton.Click += new System.EventHandler(this.restartRhinosButton_Click);
            // 
            // check_ShutDown_Condition_Timer
            // 
            this.check_ShutDown_Condition_Timer.Interval = 10000;
            this.check_ShutDown_Condition_Timer.Tick += new System.EventHandler(this.check_ShutDown_Condition_Timer_Tick);
            // 
            // Runing_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(368, 298);
            this.Controls.Add(this.restartRhinosButton);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numOfInstances_textBox);
            this.Name = "Runing_Form";
            this.Text = "Windows Rhino Controller";
            this.Load += new System.EventHandler(this.Runing_Form_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox numOfInstances_textBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button restartRhinosButton;
        private System.Windows.Forms.Timer check_ShutDown_Condition_Timer;
    }
}


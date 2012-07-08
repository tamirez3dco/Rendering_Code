namespace Process_Manager
{
    partial class Manager_Form
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.runer_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.scene_column = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.state = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.item_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.updateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rhino_pid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rune_pid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.requst_url = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ready_url = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(165, 400);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(154, 74);
            this.button1.TabIndex = 0;
            this.button1.Text = "Start All";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(634, 400);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(154, 74);
            this.button2.TabIndex = 1;
            this.button2.Text = "Kill All";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.runer_id,
            this.scene_column,
            this.state,
            this.item_id,
            this.updateTime,
            this.rhino_pid,
            this.rune_pid,
            this.requst_url,
            this.ready_url});
            this.dataGridView1.Location = new System.Drawing.Point(2, 12);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(947, 242);
            this.dataGridView1.TabIndex = 2;
            // 
            // runer_id
            // 
            this.runer_id.HeaderText = "Runer_ID";
            this.runer_id.Name = "runer_id";
            this.runer_id.ReadOnly = true;
            // 
            // scene_column
            // 
            this.scene_column.HeaderText = "Scene";
            this.scene_column.Name = "scene_column";
            this.scene_column.ReadOnly = true;
            // 
            // state
            // 
            this.state.HeaderText = "State";
            this.state.Name = "state";
            this.state.ReadOnly = true;
            // 
            // item_id
            // 
            this.item_id.HeaderText = "Item Id";
            this.item_id.Name = "item_id";
            this.item_id.ReadOnly = true;
            this.item_id.Width = 200;
            // 
            // updateTime
            // 
            this.updateTime.HeaderText = "Last Update";
            this.updateTime.Name = "updateTime";
            this.updateTime.ReadOnly = true;
            this.updateTime.Width = 200;
            // 
            // rhino_pid
            // 
            this.rhino_pid.HeaderText = "Rhino PID";
            this.rhino_pid.Name = "rhino_pid";
            this.rhino_pid.ReadOnly = true;
            // 
            // rune_pid
            // 
            this.rune_pid.HeaderText = "Runer PID";
            this.rune_pid.Name = "rune_pid";
            this.rune_pid.ReadOnly = true;
            // 
            // requst_url
            // 
            this.requst_url.HeaderText = "Request Q";
            this.requst_url.Name = "requst_url";
            this.requst_url.ReadOnly = true;
            // 
            // ready_url
            // 
            this.ready_url.HeaderText = "Ready Q";
            this.ready_url.Name = "ready_url";
            this.ready_url.ReadOnly = true;
            // 
            // Manager_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1019, 486);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Manager_Form";
            this.Text = "RhinoManager";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn runer_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn scene_column;
        private System.Windows.Forms.DataGridViewTextBoxColumn state;
        private System.Windows.Forms.DataGridViewTextBoxColumn item_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn updateTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn rhino_pid;
        private System.Windows.Forms.DataGridViewTextBoxColumn rune_pid;
        private System.Windows.Forms.DataGridViewTextBoxColumn requst_url;
        private System.Windows.Forms.DataGridViewTextBoxColumn ready_url;
    }
}


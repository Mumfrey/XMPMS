namespace DevTools
{
    partial class frmAnalyzer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAnalyzer));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lstConnections = new System.Windows.Forms.ListBox();
            this.lstPackets = new System.Windows.Forms.ListBox();
            this.txtPacketData = new System.Windows.Forms.TextBox();
            this.txtStructure = new System.Windows.Forms.TextBox();
            this.txtAnalysis = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel1.Controls.Add(this.lstConnections, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lstPackets, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtPacketData, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtStructure, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtAnalysis, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(794, 511);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // lstConnections
            // 
            this.lstConnections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstConnections.FormattingEnabled = true;
            this.lstConnections.Location = new System.Drawing.Point(3, 3);
            this.lstConnections.Name = "lstConnections";
            this.tableLayoutPanel1.SetRowSpan(this.lstConnections, 2);
            this.lstConnections.Size = new System.Drawing.Size(152, 290);
            this.lstConnections.TabIndex = 0;
            this.lstConnections.SelectedIndexChanged += new System.EventHandler(this.lstConnections_SelectedIndexChanged);
            // 
            // lstPackets
            // 
            this.lstPackets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstPackets.FormattingEnabled = true;
            this.lstPackets.Location = new System.Drawing.Point(161, 3);
            this.lstPackets.Name = "lstPackets";
            this.tableLayoutPanel1.SetRowSpan(this.lstPackets, 2);
            this.lstPackets.Size = new System.Drawing.Size(152, 290);
            this.lstPackets.TabIndex = 1;
            this.lstPackets.SelectedIndexChanged += new System.EventHandler(this.lstPackets_SelectedIndexChanged);
            // 
            // txtPacketData
            // 
            this.txtPacketData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPacketData.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPacketData.Location = new System.Drawing.Point(319, 3);
            this.txtPacketData.Multiline = true;
            this.txtPacketData.Name = "txtPacketData";
            this.txtPacketData.ReadOnly = true;
            this.txtPacketData.Size = new System.Drawing.Size(472, 96);
            this.txtPacketData.TabIndex = 2;
            // 
            // txtStructure
            // 
            this.txtStructure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtStructure.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStructure.Location = new System.Drawing.Point(319, 105);
            this.txtStructure.Multiline = true;
            this.txtStructure.Name = "txtStructure";
            this.txtStructure.Size = new System.Drawing.Size(472, 198);
            this.txtStructure.TabIndex = 3;
            this.txtStructure.TextChanged += new System.EventHandler(this.txtStructure_TextChanged);
            // 
            // txtAnalysis
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.txtAnalysis, 3);
            this.txtAnalysis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAnalysis.Location = new System.Drawing.Point(3, 309);
            this.txtAnalysis.Multiline = true;
            this.txtAnalysis.Name = "txtAnalysis";
            this.txtAnalysis.ReadOnly = true;
            this.txtAnalysis.Size = new System.Drawing.Size(788, 199);
            this.txtAnalysis.TabIndex = 4;
            // 
            // frmAnalyzer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(794, 511);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmAnalyzer";
            this.Text = "Packet Analyzer Tool";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListBox lstConnections;
        private System.Windows.Forms.ListBox lstPackets;
        private System.Windows.Forms.TextBox txtPacketData;
        private System.Windows.Forms.TextBox txtStructure;
        private System.Windows.Forms.TextBox txtAnalysis;
    }
}


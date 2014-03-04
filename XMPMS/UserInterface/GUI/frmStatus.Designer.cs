// ======================================================================
//  Unreal2 XMP Master Server
//  Copyright (C) 2010-2011  Adam Mummery-Smith
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

//  Copyright Notice:
//  Unreal and the Unreal logo are registered trademarks of Epic
//  Games, Inc. ALL RIGHTS RESERVED.
// ======================================================================

namespace XMPMS.UserInterface.GUI
{
    partial class frmStatus
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmStatus));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtConsole = new System.Windows.Forms.TextBox();
            this.lstServers = new System.Windows.Forms.ListView();
            this.chName = new System.Windows.Forms.ColumnHeader();
            this.chIP = new System.Windows.Forms.ColumnHeader();
            this.chPort = new System.Windows.Forms.ColumnHeader();
            this.chUpdated = new System.Windows.Forms.ColumnHeader();
            this.chLocal = new System.Windows.Forms.ColumnHeader();
            this.ilServerList = new System.Windows.Forms.ImageList(this.components);
            this.txtCommand = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.lblTCPPorts = new System.Windows.Forms.Label();
            this.lblWebServerPort = new System.Windows.Forms.Label();
            this.lblUpTime = new System.Windows.Forms.Label();
            this.lblActiveConnections = new System.Windows.Forms.Label();
            this.lblTotalServers = new System.Windows.Forms.Label();
            this.lblQueries = new System.Windows.Forms.Label();
            this.lblWebQueries = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtConsole, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.lstServers, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtCommand, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.label11, 0, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(797, 599);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tableLayoutPanel1.SetColumnSpan(this.label3, 3);
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label3.Location = new System.Drawing.Point(3, 229);
            this.label3.Margin = new System.Windows.Forms.Padding(3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(791, 20);
            this.label3.TabIndex = 2;
            this.label3.Text = "Console";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label2.Location = new System.Drawing.Point(500, 3);
            this.label2.Margin = new System.Windows.Forms.Padding(3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(294, 20);
            this.label2.TabIndex = 1;
            this.label2.Text = "Status";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tableLayoutPanel1.SetColumnSpan(this.label1, 2);
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Margin = new System.Windows.Forms.Padding(3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(491, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Servers";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtConsole
            // 
            this.txtConsole.BackColor = System.Drawing.Color.Black;
            this.txtConsole.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tableLayoutPanel1.SetColumnSpan(this.txtConsole, 3);
            this.txtConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtConsole.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtConsole.ForeColor = System.Drawing.Color.White;
            this.txtConsole.Location = new System.Drawing.Point(3, 255);
            this.txtConsole.Multiline = true;
            this.txtConsole.Name = "txtConsole";
            this.txtConsole.ReadOnly = true;
            this.txtConsole.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtConsole.Size = new System.Drawing.Size(791, 315);
            this.txtConsole.TabIndex = 3;
            this.txtConsole.TabStop = false;
            this.txtConsole.WordWrap = false;
            // 
            // lstServers
            // 
            this.lstServers.BackColor = System.Drawing.Color.DarkGray;
            this.lstServers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstServers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chName,
            this.chIP,
            this.chPort,
            this.chUpdated,
            this.chLocal});
            this.tableLayoutPanel1.SetColumnSpan(this.lstServers, 2);
            this.lstServers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstServers.ForeColor = System.Drawing.Color.Black;
            this.lstServers.FullRowSelect = true;
            this.lstServers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstServers.Location = new System.Drawing.Point(3, 29);
            this.lstServers.MultiSelect = false;
            this.lstServers.Name = "lstServers";
            this.lstServers.Size = new System.Drawing.Size(491, 194);
            this.lstServers.SmallImageList = this.ilServerList;
            this.lstServers.TabIndex = 4;
            this.lstServers.TabStop = false;
            this.lstServers.UseCompatibleStateImageBehavior = false;
            this.lstServers.View = System.Windows.Forms.View.Details;
            // 
            // chName
            // 
            this.chName.Text = "Name";
            this.chName.Width = 180;
            // 
            // chIP
            // 
            this.chIP.Text = "IP";
            this.chIP.Width = 100;
            // 
            // chPort
            // 
            this.chPort.Text = "Port";
            this.chPort.Width = 45;
            // 
            // chUpdated
            // 
            this.chUpdated.Text = "Updated";
            this.chUpdated.Width = 80;
            // 
            // chLocal
            // 
            this.chLocal.Text = "Local";
            // 
            // ilServerList
            // 
            this.ilServerList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilServerList.ImageStream")));
            this.ilServerList.TransparentColor = System.Drawing.Color.Transparent;
            this.ilServerList.Images.SetKeyName(0, "Unreal.ico");
            this.ilServerList.Images.SetKeyName(1, "UnrealEd.ico");
            // 
            // txtCommand
            // 
            this.txtCommand.BackColor = System.Drawing.Color.Black;
            this.tableLayoutPanel1.SetColumnSpan(this.txtCommand, 2);
            this.txtCommand.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtCommand.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCommand.ForeColor = System.Drawing.Color.White;
            this.txtCommand.Location = new System.Drawing.Point(29, 576);
            this.txtCommand.Name = "txtCommand";
            this.txtCommand.Size = new System.Drawing.Size(765, 20);
            this.txtCommand.TabIndex = 0;
            this.txtCommand.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtCommand_KeyUp);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label5, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label6, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.label7, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.label8, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.label9, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.label10, 0, 6);
            this.tableLayoutPanel2.Controls.Add(this.lblTCPPorts, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.lblWebServerPort, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.lblUpTime, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.lblActiveConnections, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.lblTotalServers, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.lblQueries, 1, 5);
            this.tableLayoutPanel2.Controls.Add(this.lblWebQueries, 1, 6);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(500, 29);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 7;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(294, 194);
            this.tableLayoutPanel2.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 27);
            this.label4.TabIndex = 0;
            this.label4.Text = "TCP Ports";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(3, 27);
            this.label5.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(108, 27);
            this.label5.TabIndex = 1;
            this.label5.Text = "Web Server Port";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(3, 54);
            this.label6.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(108, 27);
            this.label6.TabIndex = 2;
            this.label6.Text = "Up Time";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(3, 81);
            this.label7.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(108, 27);
            this.label7.TabIndex = 3;
            this.label7.Text = "Active Connections";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(3, 108);
            this.label8.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(108, 27);
            this.label8.TabIndex = 4;
            this.label8.Text = "Total Servers";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label9.Location = new System.Drawing.Point(3, 135);
            this.label9.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(108, 27);
            this.label9.TabIndex = 5;
            this.label9.Text = "Inbound Queries";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.Location = new System.Drawing.Point(3, 162);
            this.label10.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(108, 32);
            this.label10.TabIndex = 6;
            this.label10.Text = "Web Queries";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTCPPorts
            // 
            this.lblTCPPorts.AutoSize = true;
            this.lblTCPPorts.BackColor = System.Drawing.Color.DarkGray;
            this.lblTCPPorts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTCPPorts.Location = new System.Drawing.Point(120, 3);
            this.lblTCPPorts.Margin = new System.Windows.Forms.Padding(3);
            this.lblTCPPorts.Name = "lblTCPPorts";
            this.lblTCPPorts.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblTCPPorts.Size = new System.Drawing.Size(171, 21);
            this.lblTCPPorts.TabIndex = 7;
            this.lblTCPPorts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWebServerPort
            // 
            this.lblWebServerPort.AutoSize = true;
            this.lblWebServerPort.BackColor = System.Drawing.Color.DarkGray;
            this.lblWebServerPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWebServerPort.Location = new System.Drawing.Point(120, 30);
            this.lblWebServerPort.Margin = new System.Windows.Forms.Padding(3);
            this.lblWebServerPort.Name = "lblWebServerPort";
            this.lblWebServerPort.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblWebServerPort.Size = new System.Drawing.Size(171, 21);
            this.lblWebServerPort.TabIndex = 8;
            this.lblWebServerPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUpTime
            // 
            this.lblUpTime.AutoSize = true;
            this.lblUpTime.BackColor = System.Drawing.Color.DarkGray;
            this.lblUpTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpTime.Location = new System.Drawing.Point(120, 57);
            this.lblUpTime.Margin = new System.Windows.Forms.Padding(3);
            this.lblUpTime.Name = "lblUpTime";
            this.lblUpTime.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblUpTime.Size = new System.Drawing.Size(171, 21);
            this.lblUpTime.TabIndex = 9;
            this.lblUpTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblActiveConnections
            // 
            this.lblActiveConnections.AutoSize = true;
            this.lblActiveConnections.BackColor = System.Drawing.Color.DarkGray;
            this.lblActiveConnections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActiveConnections.Location = new System.Drawing.Point(120, 84);
            this.lblActiveConnections.Margin = new System.Windows.Forms.Padding(3);
            this.lblActiveConnections.Name = "lblActiveConnections";
            this.lblActiveConnections.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblActiveConnections.Size = new System.Drawing.Size(171, 21);
            this.lblActiveConnections.TabIndex = 10;
            this.lblActiveConnections.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTotalServers
            // 
            this.lblTotalServers.AutoSize = true;
            this.lblTotalServers.BackColor = System.Drawing.Color.DarkGray;
            this.lblTotalServers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalServers.Location = new System.Drawing.Point(120, 111);
            this.lblTotalServers.Margin = new System.Windows.Forms.Padding(3);
            this.lblTotalServers.Name = "lblTotalServers";
            this.lblTotalServers.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblTotalServers.Size = new System.Drawing.Size(171, 21);
            this.lblTotalServers.TabIndex = 11;
            this.lblTotalServers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQueries
            // 
            this.lblQueries.AutoSize = true;
            this.lblQueries.BackColor = System.Drawing.Color.DarkGray;
            this.lblQueries.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQueries.Location = new System.Drawing.Point(120, 138);
            this.lblQueries.Margin = new System.Windows.Forms.Padding(3);
            this.lblQueries.Name = "lblQueries";
            this.lblQueries.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblQueries.Size = new System.Drawing.Size(171, 21);
            this.lblQueries.TabIndex = 12;
            this.lblQueries.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWebQueries
            // 
            this.lblWebQueries.AutoSize = true;
            this.lblWebQueries.BackColor = System.Drawing.Color.DarkGray;
            this.lblWebQueries.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWebQueries.Location = new System.Drawing.Point(120, 165);
            this.lblWebQueries.Margin = new System.Windows.Forms.Padding(3);
            this.lblWebQueries.Name = "lblWebQueries";
            this.lblWebQueries.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblWebQueries.Size = new System.Drawing.Size(171, 26);
            this.lblWebQueries.TabIndex = 13;
            this.lblWebQueries.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.Location = new System.Drawing.Point(3, 573);
            this.label11.Name = "label11";
            this.label11.Padding = new System.Windows.Forms.Padding(0, 0, 3, 2);
            this.label11.Size = new System.Drawing.Size(20, 26);
            this.label11.TabIndex = 6;
            this.label11.Text = ">";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // frmStatus
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(797, 599);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmStatus";
            this.Text = "frmStatus";
            this.Shown += new System.EventHandler(this.HandleShown);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtConsole;
        private System.Windows.Forms.ListView lstServers;
        private System.Windows.Forms.ColumnHeader chName;
        private System.Windows.Forms.TextBox txtCommand;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lblTCPPorts;
        private System.Windows.Forms.Label lblWebServerPort;
        private System.Windows.Forms.Label lblUpTime;
        private System.Windows.Forms.Label lblActiveConnections;
        private System.Windows.Forms.Label lblTotalServers;
        private System.Windows.Forms.Label lblQueries;
        private System.Windows.Forms.Label lblWebQueries;
        private System.Windows.Forms.ColumnHeader chIP;
        private System.Windows.Forms.ImageList ilServerList;
        private System.Windows.Forms.ColumnHeader chPort;
        private System.Windows.Forms.ColumnHeader chUpdated;
        private System.Windows.Forms.ColumnHeader chLocal;
        private System.Windows.Forms.Label label11;
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using XMPMS.Net.Connections;
using XMPMS.Net.Packets;
using System.Diagnostics;

namespace DevTools
{
    public partial class frmAnalyzer : Form
    {
        protected Dictionary<TCPConnection, ConnectionData> packets = new Dictionary<TCPConnection, ConnectionData>();

        protected ConnectionData selectedConnection;

        protected PacketData selectedPacket;

        protected PacketAnalyser analyser;

        public frmAnalyzer()
        {
            InitializeComponent();

            TCPConnection.NewConnection += new ConnectionEventHandler(HandleNewConnection);
        }

        void HandleNewConnection(TCPConnection connection)
        {
            Invoke(new ConnectionEventHandler(InvokedHandleNewConnection), connection);
        }

        void InvokedHandleNewConnection(TCPConnection connection)
        {
            ConnectionData connectionData = new ConnectionData(connection);

            packets[connection] = connectionData;
            connection.ReceivedPacket += new ReceivedPacketEventHandler(HandleReceivedPacket);

            lstConnections.Items.Add(connectionData);
        }

        void HandleReceivedPacket(TCPConnection connection, InboundPacket packet)
        {
            Invoke(new ReceivedPacketEventHandler(InvokedHandleReceivedPacket), connection, packet);
        }

        void InvokedHandleReceivedPacket(TCPConnection connection, InboundPacket packet)
        {
            packets[connection].Packets.Add(new PacketData(packet));
            UpdatePacketList();
        }

        void UpdatePacketList()
        {
            lstPackets.Items.Clear();

            if (selectedConnection.Packets != null)
            {
                foreach (PacketData packet in selectedConnection.Packets)
                {
                    lstPackets.Items.Add(packet);
                }
            }
        }

        private void lstConnections_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedConnection = (ConnectionData)lstConnections.SelectedItem;
            UpdatePacketList();
        }

        private void lstPackets_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedPacket = (PacketData)lstPackets.SelectedItem;
            txtStructure.Text = selectedPacket.Analyser.Structure;
            UpdateTextBoxes();
        }

        private void txtStructure_TextChanged(object sender, EventArgs e)
        {
            if (selectedPacket != null)
            {
                selectedPacket.Analyser.Structure = txtStructure.Text;
                UpdateTextBoxes();
            }
        }

        private void UpdateTextBoxes()
        {
            if (selectedPacket != null)
            {
                txtPacketData.Text = selectedPacket.Analyser.Tail;
                txtAnalysis.Text = selectedPacket.Analyser.Result;
            }
        }
    }

    public class PacketData
    {
        public string Name;

        public InboundPacket Packet;

        public PacketAnalyser Analyser;

        public PacketData(InboundPacket packet)
        {
            Name = String.Format("{0} ({1})", DateTime.Now.ToString("dd/MM HH:mm:ss:"), packet.Length);
            Packet = packet;
            Analyser = new PacketAnalyser(packet);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public struct ConnectionData
    {
        public string Name;

        public List<PacketData> Packets;

        public ConnectionData(TCPConnection connection)
        {
            Name = String.Format("{0}:{1} {2}", connection.GetType().Name, connection.LocalPort, DateTime.Now.ToString("dd/MM HH:mm:ss:"));
            Packets = new List<PacketData>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

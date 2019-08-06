﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox;
using System.Windows.Forms;
using Toolbox.Library;
using Toolbox.Library.IO;

namespace FirstPlugin
{
    public class U8 : IArchiveFile, IFileFormat, IDirectoryContainer
    {
        public FileType FileType { get; set; } = FileType.Archive;

        public bool CanSave { get; set; }
        public string[] Description { get; set; } = new string[] { "U8" };
        public string[] Extension { get; set; } = new string[] { "*.u8"};
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public IFileInfo IFileInfo { get; set; }

        public bool CanAddFiles { get; set; }
        public bool CanRenameFiles { get; set; }
        public bool CanReplaceFiles { get; set; }
        public bool CanDeleteFiles { get; set; }

        public bool Identify(System.IO.Stream stream)
        {
            using (var reader = new Toolbox.Library.IO.FileReader(stream, true))
            {
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;

                uint signature = reader.ReadUInt32();
                reader.Position = 0;
                return signature == 0x55AA382D;
            }
        }

        public Type[] Types
        {
            get
            {
                List<Type> types = new List<Type>();
                return types.ToArray();
            }
        }

        public List<INode> nodes = new List<INode>();

        public IEnumerable<ArchiveFileInfo> Files => null;
        public IEnumerable<INode> Nodes => nodes;

        public void ClearFiles() { nodes.Clear(); }

        public string Name
        {
            get { return FileName; }
            set { FileName = value; }
        }

        private readonly uint Magic = 0x55AA382D;

        public void Load(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;

                uint Signature = reader.ReadUInt32();
                uint FirstNodeOffset = reader.ReadUInt32();
                uint NodeSectionSize = reader.ReadUInt32();
                uint FileDataOffset = reader.ReadUInt32();
                byte[] Reserved = new byte[4];

                Console.WriteLine("FirstNodeOffset " + FirstNodeOffset);

                reader.SeekBegin(FirstNodeOffset);
                var RootNode = new NodeEntry();
                RootNode.Read(reader);

                DirectoryEntry dirRoot = new DirectoryEntry();
                dirRoot.Name = "ROOT";
                dirRoot.nodeEntry = RootNode;

                //Root has total number of nodes 
                uint TotalNodeCount = RootNode.Setting2;

                List<NodeEntry> entries = new List<NodeEntry>();
                entries.Add(RootNode);
                for (int i = 0; i < TotalNodeCount - 1; i++)
                {
                    var node = new NodeEntry();
                    node.Read(reader);
                    entries.Add(node);
                }

                var directroyEntries = entries.Where(i => i.nodeType == NodeEntry.NodeType.Directory).ToArray();
                DirectoryEntry[] dirs = new DirectoryEntry[directroyEntries.Length];
                for (int i = 0; i < dirs.Length; i++)
                    dirs[i] = new DirectoryEntry();

                DirectoryEntry currentDir = dirRoot;
                nodes.Add(currentDir);

                for (int i = 0; i < TotalNodeCount; i++)
                {
                    var node = entries[i];
                    if (node.nodeType == NodeEntry.NodeType.Directory)
                    {
                        DirectoryEntry dir = new DirectoryEntry();
                        dir.nodeEntry = node;
                        dirs[node.Setting1].AddNode(dir);
                        currentDir = dir;
                    }
                    else
                    {
                        FileEntry entry = new FileEntry();
                        entry.nodeEntry = node;
                        currentDir.nodes.Add(entry);
                    }
                }

                long stringPoolPos = reader.Position;
                for (int i = 0; i < dirRoot.nodes.Count; i++)
                {

                    if (dirRoot.nodes[i] is FileEntry)
                    {
                        var file = dirRoot.nodes[i] as FileEntry;
                        reader.SeekBegin(stringPoolPos + file.nodeEntry.StringPoolOffset);
                        file.FileName = reader.ReadZeroTerminatedString();
                    }
                    else
                    {
                        var dir = dirRoot.nodes[i] as DirectoryEntry;
                        reader.SeekBegin(stringPoolPos + dir.nodeEntry.StringPoolOffset);
                        dir.Name = reader.ReadZeroTerminatedString();
                    }
                }

                for (int i = 0; i < dirRoot.nodes.Count; i++)
                {
                    if (dirRoot.nodes[i] is FileEntry)
                    {
                        var file = dirRoot.nodes[i] as FileEntry;
                        reader.SeekBegin(file.nodeEntry.Setting1);
                        file.FileData = reader.ReadBytes((int)file.nodeEntry.Setting2);
                    }
                }
            }
        }


        public void SaveFile(FileWriter writer)
        {
            long pos = writer.Position;

            writer.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;
            writer.Write(Magic);
        
        }

        public class FileEntry : ArchiveFileInfo
        {
            public NodeEntry nodeEntry;
        }

        public class DirectoryEntry : IDirectoryContainer
        {
            public NodeEntry nodeEntry;

            public string Name { get; set; }

            public IEnumerable<INode> Nodes { get { return nodes; } }
            public List<INode> nodes = new List<INode>();

            public void AddNode(INode node)
            {
                nodes.Add(node);
            }
        }

        public class NodeEntry : INode
        {
            public NodeType nodeType
            {
                get { return (NodeType)(flags >> 24); }
            }

            public enum NodeType
            {
                File,
                Directory,
            }

            public uint StringPoolOffset
            {
                get { return flags & 0x00ffffff; }
            }

            private uint flags;

            public uint Setting1; //Offset (file) or parent index (directory)
            public uint Setting2; //Size (file) or node count (directory)

            public string Name { get; set; }

            public void Read(FileReader reader)
            {
                flags = reader.ReadUInt32();
                Setting1 = reader.ReadUInt32();
                Setting2 = reader.ReadUInt32();
            }
        }

        public void Unload()
        {

        }

        public void Save(System.IO.Stream stream)
        {
            SaveFile(new FileWriter(stream));
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }
    }
}

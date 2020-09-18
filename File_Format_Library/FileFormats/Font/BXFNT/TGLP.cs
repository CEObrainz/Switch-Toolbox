﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Toolbox.Library;
using Toolbox.Library.IO;
using System.Drawing;

namespace FirstPlugin
{
    public class TGLP
    {
        public BNTX BinaryTextureFile;

        private List<STGenericTexture> textures = new List<STGenericTexture>();
        public List<STGenericTexture> Textures
        {
            get { return textures; }
            set { textures = value; }
        }

        public Bitmap GetBitmap(int index)
        {
            if (BinaryTextureFile != null)
                return GetImageSheet(index).GetBitmap(index);
            else
                return GetImageSheet(index).GetBitmap();
        }

        public uint SectionSize;
        public byte CellWidth { get; set; }
        public byte CellHeight { get; set; }
        public byte MaxCharWidth { get; set; }
        public byte SheetCount { get; private set; }
        public uint SheetSize { get; set; }
        public ushort BaseLinePos { get; set; }
        public ushort Format { get; set; }
        public ushort RowCount { get; set; }
        public ushort ColumnCount { get; set; }
        public ushort SheetWidth { get; set; }
        public ushort SheetHeight { get; set; }
        public List<byte[]> SheetDataList = new List<byte[]>();

        public void Read(FileReader reader, FFNT header)
        {
            string Signature = reader.ReadSignature(4, "TGLP");
            SectionSize = reader.ReadUInt32();
            CellWidth = reader.ReadByte();
            CellHeight = reader.ReadByte();
            if (header.Platform <= FFNT.PlatformType.Ctr && header.Version < 0x04000000)
            {
                BaseLinePos = reader.ReadByte();
                MaxCharWidth = reader.ReadByte();
                SheetSize = reader.ReadUInt32();
                SheetCount = (byte)reader.ReadUInt16();
            }
            else
            {
                SheetCount = reader.ReadByte();
                MaxCharWidth = reader.ReadByte();
                SheetSize = reader.ReadUInt32();
                BaseLinePos = reader.ReadUInt16();
            }

            Format = reader.ReadUInt16();
            RowCount = reader.ReadUInt16();
            ColumnCount = reader.ReadUInt16();
            SheetWidth = reader.ReadUInt16();
            SheetHeight = reader.ReadUInt16();

            uint sheetOffset = reader.ReadUInt32();
            using (reader.TemporarySeek(sheetOffset, SeekOrigin.Begin))
            {
                for (int i = 0; i < SheetCount; i++)
                {
                    SheetDataList.Add(reader.ReadBytes((int)SheetSize));
                    if (SheetDataList[i].Length != SheetSize)
                        throw new Exception("SheetSize mis match!");
                }
            }
        }

        public void Write(FileWriter writer, FFNT header)
        {
            long pos = writer.Position;

            if (BinaryTextureFile != null)
            {
                var mem = new System.IO.MemoryStream();
                BinaryTextureFile.Save(mem);
                SheetDataList[0] = mem.ToArray();
            }

            writer.WriteSignature("TGLP");
            writer.Write(uint.MaxValue);
            writer.Write(CellWidth);
            writer.Write(CellHeight);
            if (header.Platform <= FFNT.PlatformType.Ctr && header.Version < 0x04000000)
            {
                writer.Write((byte)BaseLinePos);
                writer.Write(MaxCharWidth);
                writer.Write(SheetDataList[0].Length);
                writer.Write((ushort)SheetDataList.Count);
            }
            else
            {
                writer.Write((byte)SheetDataList.Count);
                writer.Write(MaxCharWidth);
                writer.Write(SheetDataList[0].Length);
                writer.Write(BaseLinePos);
            }

            writer.Write(Format);
            writer.Write(RowCount);
            writer.Write(ColumnCount);
            writer.Write(SheetWidth);
            writer.Write(SheetHeight);
            long _ofsSheetBlocks = writer.Position;
            writer.Write(uint.MaxValue);

            if (header.Platform == FFNT.PlatformType.NX)
                writer.Align(4096);
            else if (header.Platform == FFNT.PlatformType.Cafe)
                writer.Align(8192);
            else if (header.Platform == FFNT.PlatformType.Ctr)
                writer.Align(64);
            else
                writer.Align(32);

            long DataPosition = writer.Position;
            using (writer.TemporarySeek(_ofsSheetBlocks, SeekOrigin.Begin))
            {
                writer.Write((uint)DataPosition);
            }

            for (int i = 0; i < SheetDataList.Count; i++)
            {
                writer.Write(SheetDataList[i]);
            }


            long SectionEndPosition = writer.Position;
            //End of section. Set the size
            using (writer.TemporarySeek(pos + 4, SeekOrigin.Begin))
            {
                writer.Write((uint)(SectionEndPosition - pos));
            }
        }

        public STGenericTexture[] GetImageSheets()
        {
            STGenericTexture[] textures = new STGenericTexture[SheetCount];
            for (int i = 0; i < SheetCount; i++)
                textures[i] = GetImageSheet(i);
            return textures;
        }

        public STGenericTexture GetImageSheet(int Index)
        {
            if (BinaryTextureFile != null) //BNTX uses only one image with multiple arrays
                return BinaryTextureFile.Textures.ElementAt(0).Value;
            else
                return Textures[Index];
        }
    }
}

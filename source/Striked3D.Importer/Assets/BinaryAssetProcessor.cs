﻿using System.IO;

namespace Striked3D.Core.Assets
{
    public abstract class BinaryAssetProcessor
    {
        public abstract object Process(Stream stream, string extension);
    }

    public abstract class BinaryAssetProcessor<T> : BinaryAssetProcessor
    {
        public override object Process(Stream stream, string extension)
        {
            return ProcessT(stream, extension);
        }

        public abstract T ProcessT(Stream stream, string extension);
    }
}

﻿using Striked3D.Math;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Striked3D.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraInfo
    {
        public Vector4D<float> viewMatrix0 { get; set; }
        public Vector4D<float> viewMatrix1 { get; set; }
        public Vector4D<float> viewMatrix2 { get; set; }
        public Vector4D<float> viewMatrix3 { get; set; }

        public Vector4D<float> projectionMatrix0 { get; set; }
        public Vector4D<float> projectionMatrix1 { get; set; }

        public Vector4D<float> projectionMatrix2 { get; set; }
        public Vector4D<float> projectionMatrix3 { get; set; }

        public float far { get; set; }
        public float near { get; set; }
        public float fov { get; set; }
        public float unknown { get; set; }

        public static uint GetSizeInBytes()
        {
            return (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(CameraInfo));
        }
    }

}

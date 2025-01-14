﻿using Veldrid;
using Striked3D.Graphics;
using Striked3D.Types;

namespace Striked3D.Core.Graphics
{
    public class World3D : World
    {
        private CameraInfo prevCamInfo;
        private DeviceBuffer CameraBuffer { get; set; }

        private bool isInit = false;

        public override void Update(IRenderer renderer, IViewport viewport)
        {
            if (!isInit)
            {
                Create(renderer);
            }

            CameraInfo currentCamInfo = viewport.ActiveCamera.CameraInfo;
            if (!prevCamInfo.Equals(currentCamInfo))
            {
                renderer.UpdateBuffer(CameraBuffer, 0, currentCamInfo);
                prevCamInfo = currentCamInfo;
            }
        }

        private void Create(IRenderer renderer)
        {
            //buffers
            CameraBuffer = renderer.CreateBuffer(new BufferDescription
            (
                CameraInfo.GetSizeInBytes(),
                BufferUsage.UniformBuffer
            ));

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(renderer.Material3DLayout, CameraBuffer);
            _resourceSet = renderer.CreateResourceSet(resourceSetDescription);

            isInit = true;
        }

        public override void Dispose()
        {
            CameraBuffer.Dispose();
            _resourceSet.Dispose();
        }

    }
}

﻿using Striked3D.Math;
using Striked3D.Core;
using Striked3D.Graphics;
using Striked3D.Importer;
using Striked3D.Resources;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Veldrid;
using System.Runtime.InteropServices;
using Striked3D.Services.Graphics;

namespace Striked3D.Nodes
{

    internal struct CanvasItem
    {
        public Material2DInfo info { get; set; }
        public Font font;
        public int atlasId = -1;
        public BitmapTexture Texture { get; set; }
    }

/// <summary>
/// A 2D Canvas Element
/// </summary>
public abstract class Canvas : Node2D, IDrawable2D
    {
        private readonly List<CanvasItem> matInfoArray = new List<CanvasItem>();

        public Material2D Material { get; set; }

        private bool _isVisible = true;

        /// <summary>
        /// The visibility of the canvas item (Disable rendering)
        /// </summary>
        /// <value>Get or set the visibility</value>
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }

        private bool needsToBeRecreate = false;

        public abstract void DrawCanvas();

        public override void OnEnterTree()
        {
            base.OnEnterTree();
            UpdateCanvas();
        }

        public void UpdateCanvas()
        {
            needsToBeRecreate = true;
        }

        public void DrawRect(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _size)
        {
            matInfoArray.Add(new CanvasItem { info = new Material2DInfo { IsFont = 0.0f, position = _position, size = _size, color = _color } });
        }

        public void DrawTextureRect(BitmapTexture texture, Vector2D<float> _position, Vector2D<float> _size)
        {
            this.DrawTextureRect(texture, _position, _size, Vector4D<float>.One);
        }

        public void DrawTextureRect(BitmapTexture texture, Vector2D<float> _position, Vector2D<float> _size, Vector4D<float> _modulate )
        {
            matInfoArray.Add(new CanvasItem { info = new Material2DInfo { IsFont = 0.0f, position = _position, size = _size, UseTexture = 1.0f, Modulate = _modulate }, Texture = texture });
        }

        public float GetTextWidth(Font font, float fontSize, string text)
        {
            if (font == null)
            {
                return 0;
            }

            float width = 0;
            float scale = (fontSize / FontImporter.renderSize);
            float size = FontImporter.renderSize * scale;

            foreach (char c in text)
            {
                FontAtlasGylph cacheChar = font.GetChar(c);
                width += (float)cacheChar.advance * scale;
            }

            return width;
        }

        public float GetTextHeight(Font font, string text, float fontSize)
        {
            if (font == null)
            {
                return 0;
            }

            float scale = fontSize / FontImporter.renderSize;
            float fontSizeScaled = FontImporter.renderSize * scale;
            float highestValue = 0;
            var highestCharValue = GetHighestChar(font, text, fontSize);
            foreach (char c in text)
            {
                FontAtlasGylph cacheChar = font.GetChar(c);

                var posY = highestCharValue;
                var size = new Vector2D<float>(fontSizeScaled, fontSizeScaled);

                posY -= cacheChar.bearing.Y * scale;
                var fontSizeY = cacheChar.size.Y * scale;

                var sizeY = cacheChar.size.Y * scale;

                highestValue = MathF.Max(highestValue, sizeY + posY);
            }

            return highestValue;
        }
        public float GetHighestChar(Font font, string text, float fontSize)
        {
            if (font == null)
            {
                return 0;
            }

            float highestValue = 0;
            float scale = fontSize / FontImporter.renderSize;

            foreach (char c in text)
            {
                FontAtlasGylph cacheChar = font.GetChar(c);
                var sizeY = cacheChar.bearing.Y * scale;
                highestValue = MathF.Max(highestValue, sizeY);
            }

            return highestValue;
        }

        public void DrawText(Font font, RgbaFloat _color, Vector2D<float> _position, float fontSize, string text)
        {
            if (font == null)
            {
                return;
            }

            float xPos = _position.X;
            float scale = fontSize / FontImporter.renderSize;
            float fontSizeScaled = FontImporter.renderSize * scale;

            var highestValue = GetHighestChar(font, text, fontSize);
            foreach (char c in text)
            {
                FontAtlasGylph cacheChar = font.GetChar(c);

                var pos = new Vector2D<float>(xPos, _position.Y + highestValue);
                var size = new Vector2D<float>(fontSizeScaled, fontSizeScaled);
       
                pos.Y -= cacheChar.bearing.Y * scale;

                var fontSizeX = cacheChar.size.X * scale;
                var fontSizeY = cacheChar.size.Y * scale;

                var sizeY = (cacheChar.size.Y * scale) + 0.5f;
                var sizeX = (cacheChar.size.X * scale) + 0.5f;

                float fromX = cacheChar.region.X;
                float fromY = cacheChar.region.Y;

                float toT = (fromX + cacheChar.size.X) + 0.5f;
                float toY = (fromY + cacheChar.size.Y) + 0.5f;

                matInfoArray.Add(new CanvasItem
                {
                    info = new Material2DInfo
                    {
                        IsFont = 1.0f,
                        position = pos,
                        size = new Vector2D<float>(sizeX, sizeY),
                      //  size = size,
                        color = _color,
                        FontRegion = new Vector4D<float>(fromX, fromY , toT, toY ),
                        FontRange = MathF.Max(1.0f, (fontSizeScaled / FontImporter.renderSize) * FontImporter.renderRange)
                    },
                    font = font,
                    atlasId = cacheChar.atlasId
                });;

                xPos += (float)( cacheChar.advance - (cacheChar.bearing.X )) * scale;
            }
        }

        public void DrawRectBorder(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _endPosition, float thickness)
        {
            //top

            Vector2D<float> leftPos1 = _position;
            Vector2D<float> leftPos2 = _position;
            leftPos2.X = _endPosition.X;

            DrawLine(_color, leftPos1, leftPos2, 0, thickness);

            //bottom

            leftPos1 = _position;
            leftPos1.Y = _endPosition.Y - (thickness / 2);
            leftPos2 = _position;
            leftPos2.X = _endPosition.X;
            leftPos2.Y = _endPosition.Y - (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, 0, thickness);


            //left

            leftPos1 = _position;
            leftPos1.X += (thickness / 2);
            leftPos2 = _position;
            leftPos2.Y = _endPosition.Y;
            leftPos2.X += (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, thickness, 0);


            //right

            leftPos1 = _position;
            leftPos1.X = _endPosition.X;
            leftPos1.X -= (thickness / 2);
            leftPos2 = _position;
            leftPos2.X = _endPosition.X;
            leftPos2.Y = _endPosition.Y;
            leftPos2.X -= (thickness / 2);

            DrawLine(_color, leftPos1, leftPos2, thickness, 0);
        }

        public void DrawLine(RgbaFloat _color, Vector2D<float> _position, Vector2D<float> _endPosition, float thicknessX, float thicknessY)
        {
            Vector2D<float> size = _endPosition - _position;
            size.Y += thicknessY;
            size.X += thicknessX;

            matInfoArray.Add(new CanvasItem { info = new Material2DInfo { IsFont = 0.0f, position = _position, size = size, color = _color } });
        }

        public float GetTextWidth(Font font, RgbaFloat _color, Vector2D<float> _position, float fontSize, string text)
        {
            return text.Length * fontSize;
        }

        public virtual void OnDraw2D(IRenderer renderer)
        {
            IMaterial mat = (Material == null) ? renderer.Default2DMaterial : Material;
            if (mat != null && !mat.isDirty)
            {
                if (matInfoArray.Count > 0)
                {
                    renderer.SetMaterial(mat);
                    renderer.SetViewport(Viewport);
                    renderer.BindBuffers(null, renderer.IndexDefaultBuffer);

                    foreach (CanvasItem item in matInfoArray)
                    {
                        if (item.info.IsFont > 0)
                        {
                            FontAtlas? atlas = item.font.GetAtlas(item.atlasId);

                            if (atlas != null)
                            {
                                renderer.SetResourceSets(new ResourceSet[] {
                                        Viewport.World2D.ResourceSet,
                                        atlas.fontAtlasSet,
                                        renderer.DefaultTextureSet
                                });
                            }

                        }
                        else
                        {
                            renderer.SetResourceSets(new ResourceSet[] {
                                    Viewport.World2D.ResourceSet,
                                    renderer.DefaultTextureSet,

                                    (item.info.UseTexture > 0 && item.Texture != null && item.Texture.bitmapTextureSet != null)
                                    ? item.Texture.bitmapTextureSet
                                    : renderer.DefaultTextureSet
                            });
                        }

                        renderer.PushConstant(item.info);
                        renderer.DrawIndexInstanced(6);
                    }
                }
            }
        }

        public void ClearCanvas()
        {
            matInfoArray.Clear();
        }

        private void CreateBuffers(IRenderer renderer)
        {
            ClearCanvas();
            DrawCanvas();
        }

        public virtual void BeforeDraw(IRenderer renderer)
        {
            foreach (Font? fontInUse in matInfoArray.Where(df => df.font != null).Select(df => df.font).Distinct())
            {
                fontInUse.Bind(renderer);
            }

            foreach (BitmapTexture? texture in matInfoArray.Where(df => df.Texture != null).Select(df => df.Texture).Distinct())
            {
                texture.BeforeDraw(renderer);
            }

            if (needsToBeRecreate)
            {
                Material?.BeforeDraw(renderer);
                CreateBuffers(renderer);

                needsToBeRecreate = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            ClearCanvas();
        }
    }
}

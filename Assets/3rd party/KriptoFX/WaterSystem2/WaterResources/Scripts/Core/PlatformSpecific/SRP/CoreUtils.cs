using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace KWS
{
  
    /// <summary>
    /// Set of utility functions for the Core Scriptable Render Pipeline Library
    /// </summary>
    internal static class CoreUtils
    {
 
        /// <summary>
        /// Clear the currently bound render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="clearFlag">Specify how the render texture should be cleared.</param>
        /// <param name="clearColor">Specify with which color the render texture should be cleared.</param>
        public static void ClearRenderTarget(CommandBuffer cmd, ClearFlag clearFlag, Color clearColor)
        {
            //if (clearFlag != ClearFlag.None)
            //    cmd.ClearRenderTarget((RTClearFlags)clearFlag, clearColor, 1.0f, 0x00);
            if (clearFlag != ClearFlag.None)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.Depth) != 0, (clearFlag & ClearFlag.Color) != 0, clearColor);
        }

        // We use -1 as a default value because when doing SPI for XR, it will bind the full texture array by default (and has no effect on 2D textures)
        // Unfortunately, for cubemaps, passing -1 does not work for faces other than the first one, so we fall back to 0 in this case.
        private static int FixupDepthSlice(int depthSlice, RTHandle buffer)
        {
            // buffer.rt can be null in case the RTHandle is constructed from a RenderTextureIdentifier.
            if (depthSlice == -1 && buffer.rt?.dimension == TextureDimension.Cube)
                depthSlice = 0;

            return depthSlice;
        }

        private static int FixupDepthSlice(int depthSlice, CubemapFace cubemapFace)
        {
            if (depthSlice == -1 && cubemapFace != CubemapFace.Unknown)
                depthSlice = 0;

            return depthSlice;
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">RenderTargetIdentifier of the render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace, depthSlice);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">RenderTargetIdentifier of the render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            SetRenderTarget(cmd, buffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color render texture.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color render texture.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">RenderTargetIdentifier of the color render texture.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace, depthSlice);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, ClearFlag.None, Color.clear);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, clearFlag, Color.clear);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">RenderTargetIdentifier of the depth render texture.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer, 0, CubemapFace.Unknown, -1);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        // Explicit load and store actions
        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(buffer, loadAction, storeAction);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        // Explicit load and store actions
        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction,
            int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            buffer = new RenderTargetIdentifier(buffer, miplevel, cubemapFace, depthSlice);
            cmd.SetRenderTarget(buffer, loadAction, storeAction);
        }

        // Explicit load and store actions
        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction,
            ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            buffer = new RenderTargetIdentifier(buffer, miplevel, cubemapFace, depthSlice);
            SetRenderTarget(cmd, buffer, loadAction, storeAction, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, ClearFlag clearFlag)
        {
            SetRenderTarget(cmd, buffer, loadAction, storeAction, clearFlag, Color.clear);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthBuffer">Depth buffer RenderTargetIdentifier.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthBuffer, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffer, colorLoadAction, colorStoreAction, depthBuffer, depthLoadAction, depthStoreAction);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthBuffer">Depth buffer RenderTargetIdentifier.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthBuffer, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction,
            int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            colorBuffer = new RenderTargetIdentifier(colorBuffer, miplevel, cubemapFace, depthSlice);
            depthBuffer = new RenderTargetIdentifier(depthBuffer, miplevel, cubemapFace, depthSlice);
            cmd.SetRenderTarget(colorBuffer, colorLoadAction, colorStoreAction, depthBuffer, depthLoadAction, depthStoreAction);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthBuffer">Depth buffer RenderTargetIdentifier.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthBuffer, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            colorBuffer = new RenderTargetIdentifier(colorBuffer, miplevel, cubemapFace, depthSlice);
            depthBuffer = new RenderTargetIdentifier(depthBuffer, miplevel, cubemapFace, depthSlice);
            SetRenderTarget(cmd, colorBuffer, colorLoadAction, colorStoreAction, depthBuffer, depthLoadAction, depthStoreAction, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">RenderTargetIdentifier of the render texture.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(buffer, colorLoadAction, colorStoreAction, depthLoadAction, depthStoreAction);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">Color buffer RenderTargetIdentifier.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthBuffer">Depth buffer RenderTargetIdentifier.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthBuffer, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlag)
        {
            SetRenderTarget(cmd, colorBuffer, colorLoadAction, colorStoreAction, depthBuffer, depthLoadAction, depthStoreAction, clearFlag, Color.clear);
        }

        private static void SetViewportAndClear(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag, Color clearColor)
        {
            // Clearing a partial viewport currently does not go through the hardware clear.
            // Instead it goes through a quad rendered with a specific shader.
            // When enabling wireframe mode in the scene view, unfortunately it overrides this shader thus breaking every clears.
            // That's why in the editor we don't set the viewport before clearing (it's set to full screen by the previous SetRenderTarget) but AFTER so that we benefit from un-bugged hardware clear.
            // We consider that the small loss in performance is acceptable in the editor.
            // A refactor of wireframe is needed before we can fix this properly (with not doing anything!)
#if !UNITY_EDITOR
            SetViewport(cmd, buffer);
#endif
            CoreUtils.ClearRenderTarget(cmd, clearFlag, clearColor);
#if UNITY_EDITOR
            SetViewport(cmd, buffer);
#endif
        }

        // This set of RenderTarget management methods is supposed to be used when rendering RTHandle render texture.
        // This will automatically set the viewport based on the RTHandle System reference size and the RTHandle scaling info.

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="buffer">Color buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, buffer);
            cmd.SetRenderTarget(buffer.nameID, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, buffer, clearFlag, clearColor);
        }

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="buffer">Color buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
            => SetRenderTarget(cmd, buffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="colorBuffer">Color buffer RTHandle</param>
        /// <param name="depthBuffer">Depth buffer RTHandle</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle colorBuffer, RTHandle depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            if (colorBuffer.rt != null && depthBuffer.rt != null)
            {
                int cw = colorBuffer.rt.width;
                int ch = colorBuffer.rt.height;
                int dw = depthBuffer.rt.width;
                int dh = depthBuffer.rt.height;

                Debug.Assert(cw == dw && ch == dh);
            }

            SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="colorBuffer">Color buffer RTHandle</param>
        /// <param name="depthBuffer">Depth buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle colorBuffer, RTHandle depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            if (colorBuffer.rt != null && depthBuffer.rt != null)
            {
                int cw = colorBuffer.rt.width;
                int ch = colorBuffer.rt.height;
                int dw = depthBuffer.rt.width;
                int dh = depthBuffer.rt.height;

                Debug.Assert(cw == dw && ch == dh);
            }

            SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        /// <summary>
        /// Setup the current render texture using an RTHandle
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands</param>
        /// <param name="colorBuffer">Color buffer RTHandle</param>
        /// <param name="depthBuffer">Depth buffer RTHandle</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle colorBuffer, RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            if (colorBuffer.rt != null && depthBuffer.rt != null)
            {
                int cw = colorBuffer.rt.width;
                int ch = colorBuffer.rt.height;
                int dw = depthBuffer.rt.width;
                int dh = depthBuffer.rt.height;

                Debug.Assert(cw == dw && ch == dh);
            }

            SetRenderTarget(cmd, colorBuffer.nameID, depthBuffer.nameID, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, colorBuffer, clearFlag, clearColor);
        }

        // Explicit load and store actions
        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="buffer">Color buffer RTHandleRTR.</param>
        /// <param name="loadAction">Load action.</param>
        /// <param name="storeAction">Store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle buffer, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            SetRenderTarget(cmd, buffer.nameID, loadAction, storeAction, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, buffer, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffer">Color buffer RTHandle.</param>
        /// <param name="colorLoadAction">Color buffer load action.</param>
        /// <param name="colorStoreAction">Color buffer store action.</param>
        /// <param name="depthBuffer">Depth buffer RTHandle.</param>
        /// <param name="depthLoadAction">Depth buffer load action.</param>
        /// <param name="depthStoreAction">Depth buffer store action.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        /// <param name="miplevel">Mip level that should be bound as a render texture if applicable.</param>
        /// <param name="cubemapFace">Cubemap face that should be bound as a render texture if applicable.</param>
        /// <param name="depthSlice">Depth slice that should be bound as a render texture if applicable.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RTHandle colorBuffer, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RTHandle depthBuffer, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            if (colorBuffer.rt != null && depthBuffer.rt != null)
            {
                int cw = colorBuffer.rt.width;
                int ch = colorBuffer.rt.height;
                int dw = depthBuffer.rt.width;
                int dh = depthBuffer.rt.height;

                Debug.Assert(cw == dw && ch == dh);
            }

            SetRenderTarget(cmd, colorBuffer.nameID, colorLoadAction, colorStoreAction, depthBuffer.nameID, depthLoadAction, depthStoreAction, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, colorBuffer, clearFlag, clearColor);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">Depth Buffer RTHandle.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer.nameID, ClearFlag.None, Color.clear);
            SetViewport(cmd, depthBuffer);
        }

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">Depth Buffer RTHandle.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer.nameID); // Don't clear here, viewport needs to be set before we do.
            SetViewportAndClear(cmd, depthBuffer, clearFlag, Color.clear);
        }
        

        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">Depth Buffer RTHandle.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer.nameID, 0, CubemapFace.Unknown, -1);
            SetViewportAndClear(cmd, depthBuffer, clearFlag, clearColor);
        }
        
        /// <summary>
        /// Set the current multiple render texture.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="colorBuffers">RenderTargetIdentifier array of the color render textures.</param>
        /// <param name="depthBuffer">Depth Buffer RTHandle.</param>
        /// <param name="clearFlag">If not set to ClearFlag.None, specifies how to clear the render target after setup.</param>
        /// <param name="clearColor">If applicable, color with which to clear the render texture after setup.</param>
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            depthSlice = FixupDepthSlice(depthSlice, cubemapFace);
            cmd.SetRenderTarget(colorBuffers, depthBuffer.nameID, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, depthBuffer, clearFlag, clearColor);
        }

        // Scaling viewport is done for auto-scaling render targets.
        // In the context of SRP, every auto-scaled RT is scaled against the maximum RTHandles reference size (that can only grow).
        // When we render using a camera whose viewport is smaller than the RTHandles reference size (and thus smaller than the RT actual size), we need to set it explicitly (otherwise, native code will set the viewport at the size of the RT)
        // For auto-scaled RTs (like for example a half-resolution RT), we need to scale this viewport accordingly.
        // For non scaled RTs we just do nothing, the native code will set the viewport at the size of the RT anyway.

        /// <summary>
        /// Setup the viewport to the size of the provided RTHandle.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for rendering commands.</param>
        /// <param name="target">RTHandle from which to compute the proper viewport.</param>
        public static void SetViewport(CommandBuffer cmd, RTHandle target)
        {
            if (target.useScaling)
            {
                Vector2Int scaledViewportSize = target.GetScaledSize(target.rtHandleProperties.currentViewportSize);
                cmd.SetViewport(new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y));
            }
        }

       
    }
}

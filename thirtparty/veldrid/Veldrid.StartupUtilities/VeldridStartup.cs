﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid.Sdl2;

namespace Veldrid.StartupUtilities
{
    public static class VeldridStartup
    {
        public static void CreateWindowAndGraphicsDevice(
            WindowCreateInfo windowCI,
            out Sdl2Window window,
            out GraphicsDevice gd)
            => CreateWindowAndGraphicsDevice(
                windowCI,
                new GraphicsDeviceOptions(),
                GetPlatformDefaultBackend(),
                out window,
                out gd);

        public static void CreateWindowAndGraphicsDevice(
            WindowCreateInfo windowCI,
            GraphicsDeviceOptions deviceOptions,
            out Sdl2Window window,
            out GraphicsDevice gd)
            => CreateWindowAndGraphicsDevice(windowCI, deviceOptions, GetPlatformDefaultBackend(), out window, out gd);

        public static void CreateWindowAndGraphicsDevice(
            WindowCreateInfo windowCI,
            GraphicsDeviceOptions deviceOptions,
            GraphicsBackend preferredBackend,
            out Sdl2Window window,
            out GraphicsDevice gd)
        {
            Sdl2Native.SDL_Init(SDLInitFlags.Video);
            if (preferredBackend == GraphicsBackend.OpenGL || preferredBackend == GraphicsBackend.OpenGLES)
            {
                SetSDLGLContextAttributes(deviceOptions, preferredBackend);
            }

            window = CreateWindow(ref windowCI);
            gd = CreateGraphicsDevice(window, deviceOptions, preferredBackend);
        }


        public static Sdl2Window CreateWindow(WindowCreateInfo windowCI) => CreateWindow(ref windowCI);

        public static Sdl2Window CreateWindow(ref WindowCreateInfo windowCI)
        {
            SDL_WindowFlags flags = SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable
                    | GetWindowFlags(windowCI.WindowInitialState);
            if (windowCI.WindowInitialState != WindowState.Hidden)
            {
                flags |= SDL_WindowFlags.Shown;
            }
            Sdl2Window window = new Sdl2Window(
                windowCI.WindowTitle,
                windowCI.X,
                windowCI.Y,
                windowCI.WindowWidth,
                windowCI.WindowHeight,
                flags,
                false);

            return window;
        }

        private static SDL_WindowFlags GetWindowFlags(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    return 0;
                case WindowState.FullScreen:
                    return SDL_WindowFlags.Fullscreen;
                case WindowState.Maximized:
                    return SDL_WindowFlags.Maximized;
                case WindowState.Minimized:
                    return SDL_WindowFlags.Minimized;
                case WindowState.BorderlessFullScreen:
                    return SDL_WindowFlags.FullScreenDesktop;
                case WindowState.Hidden:
                    return SDL_WindowFlags.Hidden;
                default:
                    throw new VeldridException("Invalid WindowState: " + state);
            }
        }

        public static GraphicsDevice CreateGraphicsDevice(Sdl2Window window)
            => CreateGraphicsDevice(window, new GraphicsDeviceOptions(), GetPlatformDefaultBackend());
        public static GraphicsDevice CreateGraphicsDevice(Sdl2Window window, GraphicsDeviceOptions options)
            => CreateGraphicsDevice(window, options, GetPlatformDefaultBackend());
        public static GraphicsDevice CreateGraphicsDevice(Sdl2Window window, GraphicsBackend preferredBackend)
            => CreateGraphicsDevice(window, new GraphicsDeviceOptions(), preferredBackend);
        public static GraphicsDevice CreateGraphicsDevice(
            Sdl2Window window,
            GraphicsDeviceOptions options,
            GraphicsBackend preferredBackend)
        {
            switch (preferredBackend)
            {
                case GraphicsBackend.Direct3D11:
#if !EXCLUDE_D3D11_BACKEND
                    return CreateDefaultD3D11GraphicsDevice(options, window);
#else
                    throw new VeldridException("D3D11 support has not been included in this configuration of Veldrid");
#endif
                case GraphicsBackend.Vulkan:
#if !EXCLUDE_VULKAN_BACKEND
                    return CreateVulkanGraphicsDevice(options, window);
#else
                    throw new VeldridException("Vulkan support has not been included in this configuration of Veldrid");
#endif

                case GraphicsBackend.Metal:
#if !EXCLUDE_METAL_BACKEND
                    return CreateMetalGraphicsDevice(options, window);
#else
                    throw new VeldridException("Metal support has not been included in this configuration of Veldrid");
#endif

                default:
                    throw new VeldridException("Invalid GraphicsBackend: " + preferredBackend);
            }
        }

        public static unsafe SwapchainSource GetSwapchainSource(Sdl2Window window)
        {
            IntPtr sdlHandle = window.SdlWindowHandle;
            SDL_SysWMinfo sysWmInfo;
            Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
            Sdl2Native.SDL_GetWMWindowInfo(sdlHandle, &sysWmInfo);
            switch (sysWmInfo.subsystem)
            {
                case SysWMType.Windows:
                    Win32WindowInfo w32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
                    return SwapchainSource.CreateWin32(w32Info.Sdl2Window, w32Info.hinstance);
                case SysWMType.X11:
                    X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
                    return SwapchainSource.CreateXlib(
                        x11Info.display,
                        x11Info.Sdl2Window);
                case SysWMType.Wayland:
                    WaylandWindowInfo wlInfo = Unsafe.Read<WaylandWindowInfo>(&sysWmInfo.info);
                    return SwapchainSource.CreateWayland(wlInfo.display, wlInfo.surface);
                case SysWMType.Cocoa:
                    CocoaWindowInfo cocoaInfo = Unsafe.Read<CocoaWindowInfo>(&sysWmInfo.info);
                    IntPtr nsWindow = cocoaInfo.Window;
                    return SwapchainSource.CreateNSWindow(nsWindow);
                default:
                    throw new PlatformNotSupportedException("Cannot create a SwapchainSource for " + sysWmInfo.subsystem + ".");
            }
        }

#if !EXCLUDE_METAL_BACKEND
        private static unsafe GraphicsDevice CreateMetalGraphicsDevice(GraphicsDeviceOptions options, Sdl2Window window)
            => CreateMetalGraphicsDevice(options, window, options.SwapchainSrgbFormat);
        private static unsafe GraphicsDevice CreateMetalGraphicsDevice(
            GraphicsDeviceOptions options,
            Sdl2Window window,
            bool colorSrgb)
        {
            SwapchainSource source = GetSwapchainSource(window);
            SwapchainDescription swapchainDesc = new SwapchainDescription(
                source,
                (uint)window.Width, (uint)window.Height,
                options.SwapchainDepthFormat,
                options.SyncToVerticalBlank,
                colorSrgb);

            return GraphicsDevice.CreateMetal(options, swapchainDesc);
        }
#endif

        public static GraphicsBackend GetPlatformDefaultBackend()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GraphicsBackend.Direct3D11;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal)
                    ? GraphicsBackend.Metal
                    : GraphicsBackend.OpenGL;
            }
            else
            {
                return GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                    ? GraphicsBackend.Vulkan
                    : GraphicsBackend.OpenGL;
            }
        }

#if !EXCLUDE_VULKAN_BACKEND
        public static unsafe GraphicsDevice CreateVulkanGraphicsDevice(GraphicsDeviceOptions options, Sdl2Window window)
            => CreateVulkanGraphicsDevice(options, window, false);
        public static unsafe GraphicsDevice CreateVulkanGraphicsDevice(
            GraphicsDeviceOptions options,
            Sdl2Window window,
            bool colorSrgb)
        {
            SwapchainDescription scDesc = new SwapchainDescription(
                GetSwapchainSource(window),
                (uint)window.Width,
                (uint)window.Height,
                options.SwapchainDepthFormat,
                options.SyncToVerticalBlank,
                colorSrgb);
            GraphicsDevice gd = GraphicsDevice.CreateVulkan(options, scDesc);

            return gd;
        }

        private static unsafe Veldrid.Vk.VkSurfaceSource GetSurfaceSource(SDL_SysWMinfo sysWmInfo)
        {
            switch (sysWmInfo.subsystem)
            {
                case SysWMType.Windows:
                    Win32WindowInfo w32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
                    return Vk.VkSurfaceSource.CreateWin32(w32Info.hinstance, w32Info.Sdl2Window);
                case SysWMType.X11:
                    X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
                    return Vk.VkSurfaceSource.CreateXlib(
                        (Vulkan.Xlib.Display*)x11Info.display,
                        new Vulkan.Xlib.Window() { Value = x11Info.Sdl2Window });
                default:
                    throw new PlatformNotSupportedException("Cannot create a Vulkan surface for " + sysWmInfo.subsystem + ".");
            }
        }
#endif

#if !EXCLUDE_D3D11_BACKEND
        public static GraphicsDevice CreateDefaultD3D11GraphicsDevice(
            GraphicsDeviceOptions options,
            Sdl2Window window)
        {
            SwapchainSource source = GetSwapchainSource(window);
            SwapchainDescription swapchainDesc = new SwapchainDescription(
                source,
                (uint)window.Width, (uint)window.Height,
                options.SwapchainDepthFormat,
                options.SyncToVerticalBlank,
                options.SwapchainSrgbFormat);

            return GraphicsDevice.CreateD3D11(options, swapchainDesc);
        }
#endif

        private static unsafe string GetString(byte* stringStart)
        {
            int characters = 0;
            while (stringStart[characters] != 0)
            {
                characters++;
            }

            return Encoding.UTF8.GetString(stringStart, characters);
        }

    }
}

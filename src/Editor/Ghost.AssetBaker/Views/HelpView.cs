using System;
using System.Collections.Generic;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views;

public class HelpView : Component
{
    public override Element Render()
    {
        return ScrollView(
            FlexColumn(
                // Page Header
                Heading("GhostEngine Asset Integration Guide"),
                Caption("Learn how to load your baked assets directly into the GhostEngine runtime.")
                    .Foreground(Theme.SecondaryText),
                
                Border(Empty()).Height(1).Background(Theme.DividerStroke),

                // Introduction
                Body("This Asset Baker transforms standard formats (FBX, PNG, HLSL) into lightweight, hardware-ready, and AOT-compatible files tailored specifically for the D3D12 render hardware interface (RHI)."),

                // 1. Mesh Loading
                DocSection(
                    "Loading Baked Meshes (.gmesh)",
                    "Baked meshes contain optimized vertex streams, pre-computed normals, tangents, bounding shapes, and optional LOD indices.",
                    @"// Load mesh resource from file
var meshHandle = AssetDatabase.LoadMesh(""Assets/player.gmesh"");

// Assign to ECS Entity
var entity = entityManager.CreateEntity();
entityManager.AddComponent(entity, new MeshComponent { Mesh = meshHandle });"
                ),

                // 2. Texture Loading
                DocSection(
                    "Loading Baked Textures (.gtex)",
                    "Textures are compressed into GPU-native block compression formats (e.g. BC7 on PC) with mipmaps baked directly to disk to minimize loading times and GPU decode overhead.",
                    @"// Load texture resource
var texHandle = AssetDatabase.LoadTexture(""Assets/bricks_albedo.gtex"");

// Set up material parameters
var material = new Material(shaderHandle);
material.SetTexture(""AlbedoMap"", texHandle);"
                ),

                // 3. Package Bundle loading
                DocSection(
                    "Using Bundled Packages (.gpak)",
                    "GPak bundles combine multiple asset streams into a single contiguous archive. This is the recommended structure for final game builds as it supports high-speed streaming and avoids file-handle count limits.",
                    @"// Register package at engine startup
AssetDatabase.RegisterPackage(""Packages/CoreAssets.gpak"");

// Now files can be loaded using relative virtual paths
var mesh = AssetDatabase.LoadMesh(""player.gmesh"");"
                )

            ) with { RowGap = 20 }
        ).Padding(horizontal: 24, vertical: 8);
    }

    private static Element DocSection(string title, string description, string codeSnippet)
    {
        return FlexColumn(
            SubHeading(title),
            Body(description)
                .Margin(bottom: 8),
            (Border(
                TextBlock(codeSnippet)
                    .FontFamily("Consolas")
                    .FontSize(12)
                    .Foreground(Theme.Ref("CodeBlockForegroundBrush"))
                    .TextWrapping(TextWrapping.NoWrap)
            ) with
            {
                BorderThickness = 1,
                CornerRadius = 6,
                ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", Theme.CardStroke } }
            })
            .Padding(16)
            .Background(Theme.Ref("CodeBlockBackgroundBrush"))
        ) with { RowGap = 4 };
    }
}

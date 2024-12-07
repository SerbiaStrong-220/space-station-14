// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Animations;
using Content.Client.SS220.StyleTools;
using Content.Shared.SS220.Maths;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.RoundEnd.UI;

public sealed class RoundEndTitlesStyle : QuickStyle
{
    private const float WindowFadeInDuration = 2f;
    public Animation FadeInAnimation { get; } = new()
    {
        Length = TimeSpan.FromSeconds(WindowFadeInDuration),
        AnimationTracks =
        {
            new AnimationTrackEasing
            {
                Easing = Easing.OutCubic,
                Duration = TimeSpan.FromSeconds(WindowFadeInDuration),
                Track = new AnimationTrackControlProperty
                {
                    Property = nameof(Control.Modulate),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0), 0f),
                        new AnimationTrackProperty.KeyFrame(Color.White, WindowFadeInDuration),
                    }
                }
            }
        }
    };

    private const string TuffyRegular = "/Fonts/SS220/Tuffy/Tuffy-Regular.ttf";
    private const string TuffyBold = "/Fonts/SS220/Tuffy/Tuffy-Bold.ttf";
    private const int NormalFontSize = 14;
    private const int SponsorFontSize = 10;


    protected override void CreateRules()
    {
        Builder.Element<PanelContainer>()
            .Class("RoundEndTitlesBackground")
            .Prop(PanelContainer.StylePropertyPanel, new StyleBoxTexture()
            {
                PatchMarginTop = 6f,
                PatchMarginRight = 6f,
                PatchMarginBottom = 6f,
                PatchMarginLeft = 6f,
                Texture = Tex("/Textures/SS220/Interface/RoundEnd/RoundEndTitlesBackround.png"),
                TextureScale = new(2f),
            });
        Builder.Element<Label>()
            .Class("RoundEndTitlesHeader1")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyBold, NormalFontSize));
        Builder.Element<Label>()
            .Class("RoundEndTitlesHeader2")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyBold, NormalFontSize));
        Builder.Element<Label>()
            .Class("RoundEndTitlesEpisode")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyRegular, NormalFontSize));
        Builder.Element<Label>()
            .Class("RoundEndTitlesGamemode")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyRegular, NormalFontSize));
        Builder.Element<Label>()
            .Class("RoundEndTitlesUsername")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyRegular, NormalFontSize));
        Builder.Element<Label>()
            .Class("RoundEndTitlesRole")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyRegular, NormalFontSize));
        Builder.Element<Label>()
            .Class("RoundEndTitlesName")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyRegular, NormalFontSize));
        Builder.Element<Label>()
            .Class("RoundEndTitlesSponsorName")
            .Prop(Label.StylePropertyFont, VectorFont(TuffyRegular, SponsorFontSize));
        Builder.Element<RoundEndTitlesRole>()
            .Prop("SetHeight", 40f);
    }

    public Texture GetDepartmentIcon(string prototypeId)
    {
        return Tex(new ResPath("/Textures/SS220/DepartmentIcons") / prototypeId switch
        {
            "Cargo" => "Cargo_dep.png",
            "Civilian" => "Serv_dep.png",
            "Command" => "Cmd_dep.png",
            "CentralCommand" => "Centcom_dep.png",
            "Engineering" => "Eng_dep.png",
            "Medical" => "Med_dep.png",
            "Security" => "Sec_dep.png",
            "Science" => "Sci_dep.png",
            "Cryo" => "empty.png",
            "Silicon" => "Silicon_dep.png",
            "GhostRoles" => "Ghost_dep.png",
            "Law" => "Law_dep.png",
            _ => "empty.png",
        });
    }

    public Texture GetAntagIcon()
    {
        return Tex(new ResPath("/Textures/SS220/DepartmentIcons/Antag_dep.png"));
    }

    public float GetScrollingSpeed(TimeSpan time)
    {
        var normalSpeed = 70f;
        var speedUpDuration = 3f;
        var easing = Easings.InSine;
        return easing(Math.Min((float)time.TotalSeconds / speedUpDuration, 1f)) * normalSpeed;
    }
}

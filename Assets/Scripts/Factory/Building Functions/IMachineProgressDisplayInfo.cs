using UnityEngine;

public interface IMachineProgressDisplayInfo
{
    bool HasProgressDisplay { get; }
    bool UseQuestionMarkSprite { get; }
    Sprite ProgressDisplaySprite { get; }
    Color ProgressDisplayTint { get; }
}
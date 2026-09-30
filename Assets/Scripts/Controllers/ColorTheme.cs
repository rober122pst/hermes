using UnityEngine;

[CreateAssetMenu(fileName = "ColorTheme", menuName = "UI/ColorTheme")]
public class ColorTheme : ScriptableObject
{
    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.gray;
    public Color backgroundColor = Color.gray5;
}

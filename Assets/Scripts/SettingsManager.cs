using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public Slider speedSlider;
    public Slider volumeSlider;
    public Toggle menuMusicToggle;
    public Toggle subtitlesToggle;
    public Toggle highlightToggle;
    
    private void Start()
    {
        speedSlider.value = PlayerPrefs.GetFloat("Speed", 1f);
        GameManager.Instance.gameSpeed = speedSlider.value;
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
        GameManager.Instance.gameVolume = volumeSlider.value;
        menuMusicToggle.isOn = PlayerPrefs.GetInt("MenuMusic", 1) == 1;
        GameManager.Instance.enableMenuMusic = menuMusicToggle.isOn;
        subtitlesToggle.isOn = PlayerPrefs.GetInt("Subtitles", 0) == 1;
        GameManager.Instance.showSubtitles = subtitlesToggle.isOn;
        GameManager.Instance.highlightFirstButton = highlightToggle.isOn;
        GameManager.Instance.currentLayout?.HighlightButton(highlightToggle.isOn);
    }

    public void SaveChanges()
    {
        PlayerPrefs.SetFloat("Speed", speedSlider.value);
        GameManager.Instance.gameSpeed = speedSlider.value;
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        GameManager.Instance.gameVolume = volumeSlider.value;
        PlayerPrefs.SetInt("MenuMusic", menuMusicToggle.isOn ? 1 : 0);
        GameManager.Instance.enableMenuMusic = menuMusicToggle.isOn;
        PlayerPrefs.SetInt("Subtitles", subtitlesToggle.isOn ? 1 : 0);
        GameManager.Instance.showSubtitles = subtitlesToggle.isOn;
        GameManager.Instance.highlightFirstButton = highlightToggle.isOn;
        GameManager.Instance.currentLayout?.HighlightButton(highlightToggle.isOn);
    }
}

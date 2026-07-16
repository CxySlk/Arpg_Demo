using LLKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIWindow(nameof(UI_SettingWindow), nameof(UI_SettingWindow), true, 4)]
public class UI_SettingWindow : UI_WindowBase
{
    [SerializeField] private Slider sliderBgMusic;
    [SerializeField] private TMP_Text txtBgMusic;

    [SerializeField] private Slider sliderEffMusic;
    [SerializeField] private TMP_Text txtEffMusic;

    [SerializeField] private Slider sliderGlobalMusic;
    [SerializeField] private TMP_Text txtGlobalMusic;

    [SerializeField] private Button btnBack;

    [SerializeField] private AudioClip volumeSlideClip;

    private VolumeData VolumeData => DataManager.VolumeData;

    public override void OnShow()
    {
        // 滑动条更新
        sliderBgMusic.value = VolumeData.bgVolume * 100;
        sliderEffMusic.value = VolumeData.effVolume * 100;
        sliderGlobalMusic.value = VolumeData.globalVolume * 100;
        // 音量文本更新
        txtBgMusic.text = (VolumeData.bgVolume * 100).ToString();
        txtEffMusic.text = (VolumeData.effVolume * 100).ToString();
        txtGlobalMusic.text = (VolumeData.globalVolume * 100).ToString();

        sliderBgMusic.onValueChanged.AddListener(BgMusicVolumeChanged);
        sliderEffMusic.onValueChanged.AddListener(EffMusicVolumeChanged);
        sliderGlobalMusic.onValueChanged.AddListener(GlobalMusicVolumeChanged);
        btnBack.onClick.AddListener(OnButtonBackClick);
    }

    protected override void OnClose()
    {
        sliderBgMusic.onValueChanged.RemoveAllListeners();
        sliderEffMusic.onValueChanged.RemoveAllListeners();
        sliderGlobalMusic.onValueChanged.RemoveAllListeners();
        btnBack.onClick.RemoveAllListeners();
    }
    #region 事件监听
    private void BgMusicVolumeChanged(float volume)
    {
        txtBgMusic.text = volume.ToString();
        // 真实音量
        volume /= 100;
        AudioSystem.PlayEffectMusicByRealVolume(volumeSlideClip, volume * VolumeData.globalVolume);
        AudioSystem.SetBgMusicVolume(volume);
        DataManager.SetBgVolume(volume);
    }
    private void EffMusicVolumeChanged(float volume)
    {
        txtEffMusic.text = volume.ToString();
        // 真实音量
        volume /= 100;
        AudioSystem.PlayEffectMusicByRealVolume(volumeSlideClip, volume * VolumeData.globalVolume);
        AudioSystem.SetEffMusicVolume(volume);
        DataManager.SetEffVolume(volume);
    }
    private void GlobalMusicVolumeChanged(float volume)
    {
        txtGlobalMusic.text = volume.ToString();
        volume /= 100;
        AudioSystem.PlayEffectMusicByRealVolume(volumeSlideClip, volume);
        AudioSystem.SetGlobalVolume(volume);
        DataManager.SetGlobalVolume(volume);
    }
    private void OnButtonBackClick()
    {
        AudioSystem.PlayEffectMusic("ButtonClick");
        DataManager.SaveVolumeData();
        UISystem.Close<UI_SettingWindow>();
    }
    #endregion
}

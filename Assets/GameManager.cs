using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RavSoft.GoogleTranslator;
using TMPro;
using static TMPro.TMP_Dropdown;
using static ColorType;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class GameManager : MonoBehaviour
{
    const string ResultLanguage = "ResultLanguage";
    const string OriginalLanguage = "OriginalLanguage";
    const string OrigPath = "OrigPath";
    const string ResultPath = "ResultPath";


    private Translator translator = new Translator();
    private static GameManager instance = null;

    [Header("YML mode")] [Space] [Header("Original")] [SerializeField]
    private GameObject YML_modePanel;

    [SerializeField] private TMP_InputField originalPathText;

    [SerializeField] private TMP_Dropdown originalLanguageDropdown_YML_mode;

    [Header("Result")] [SerializeField] private TMP_InputField resultPathText;
    [SerializeField] private TMP_Dropdown resultLanguageDropdown_YML_mode;

    [Space(10)] [Header("Code mode")] [Space] [SerializeField]
    private GameObject code_modePanel;

    [SerializeField] private TMP_InputField originalName;
    [SerializeField] private TMP_InputField originalDescription;
    [SerializeField] private TMP_Dropdown originalLanguageDropdown_code_mode;
    [SerializeField] private TMP_Dropdown resultLanguageDropdown_code_mode;


    [Space(10)] [Header("Other")] [SerializeField]
    private Button YML_modeButton;

    [SerializeField] private Button code_modeButton;
    [SerializeField] private Button translateButton;

    [Space(10)] [SerializeField] private Button stopTranslationButton;

    [SerializeField] private List<OptionData> optionDatas = new List<OptionData>()
    {
        new("-ALL-"),
        new("English"),
        new("Swedish"),
        new("French"),
        new("Italian"),
        new("German"),
        new("Spanish"),
        new("Russian"),
        new("Romanian"),
        new("Bulgarian"),
        new("Macedonian"),
        new("Finnish"),
        new("Danish"),
        new("Norwegian"),
        new("Icelandic"),
        new("Turkish"),
        new("Lithuanian"),
        new("Czech"),
        new("Hungarian"),
        new("Slovak"),
        new("Polish"),
        new("Dutch"),
        new("Portuguese"),
        new("Chinese"),
        new("Japanese"),
        new("Korean"),
        new("Hindi"),
        new("Thai"),
        new("Abenaki"),
        new("Croatian"),
        new("Georgian"),
        new("Greek"),
        new("Serbian"),
        new("Ukrainian")
    };

    [Space(10)] [Header("Debug")] [SerializeField]
    private TMP_Text debugText;


    private string resultPath => resultPathText.text;
    private string origPath => originalPathText.text;
    private string origPathFile => origPath + $"\\{origLanguage_YML}.yml";

    private string origLanguage_YML =>
        originalLanguageDropdown_YML_mode.options[originalLanguageDropdown_YML_mode.value].text;

    private string resultLanguage_YML =>
        resultLanguageDropdown_YML_mode.options[resultLanguageDropdown_YML_mode.value].text;

    private string origLanguage_Code =>
        originalLanguageDropdown_code_mode.options[originalLanguageDropdown_code_mode.value].text;

    private string resultLanguage_Code =>
        resultLanguageDropdown_code_mode.options[resultLanguageDropdown_code_mode.value].text;

    private void Awake()
    {
        instance = this;
        debugText.text = string.Empty;
        originalPathText.onEndEdit.AddListener(_ => CheckFile(origPathFile));
        resultPathText.onEndEdit.AddListener(_ => CheckDirectory(resultPath));
        translateButton.onClick.AddListener(TryTranslate);
        stopTranslationButton.onClick.AddListener((() =>
        {
            StopAllCoroutines();
            DoDebug("Stoped by user", red, 4);
        }));

        resultLanguageDropdown_YML_mode.AddOptions(optionDatas);
        resultLanguageDropdown_YML_mode.onValueChanged.AddListener((_ => { CheckDirectory(resultPath); }));
        resultLanguageDropdown_YML_mode.onValueChanged.AddListener((_ =>
        {
            PlayerPrefs.SetString(ResultLanguage, resultLanguage_YML);
            if (origLanguage_YML == resultLanguage_YML)
                DoDebug($"Original language equals to result language: {resultLanguage_YML}", yellow, 3);
        }));
        originalLanguageDropdown_YML_mode.AddOptions(optionDatas.Where(x => x.text != "-ALL-").ToList());
        originalLanguageDropdown_YML_mode.onValueChanged.AddListener((_ => { CheckFile(origPathFile); }));
        originalLanguageDropdown_YML_mode.onValueChanged.AddListener((_ =>
        {
            PlayerPrefs.SetString(OriginalLanguage, origLanguage_YML);
            if (origLanguage_YML == resultLanguage_YML)
                DoDebug($"Original language equals to result language: {resultLanguage_YML}", yellow, 3);
        }));

        if (PlayerPrefs.HasKey(OrigPath)) originalPathText.text = PlayerPrefs.GetString(OrigPath);
        if (PlayerPrefs.HasKey(ResultPath)) resultPathText.text = PlayerPrefs.GetString(ResultPath);
        if (PlayerPrefs.HasKey(ResultLanguage))
            resultLanguageDropdown_YML_mode.value = resultLanguageDropdown_YML_mode.options.IndexOf(
                resultLanguageDropdown_YML_mode.options.Find(x => x.text == PlayerPrefs.GetString(ResultLanguage)));

        YML_modeButton.onClick.AddListener((() =>
        {
            YML_modeButton.interactable = false;
            code_modeButton.interactable = true;

            YML_modePanel.SetActive(true);
            code_modePanel.SetActive(false);
        }));
        code_modeButton.onClick.AddListener((() =>
        {
            YML_modeButton.interactable = true;
            code_modeButton.interactable = false;

            YML_modePanel.SetActive(false);
            code_modePanel.SetActive(true);
        }));


        CheckFile(origPathFile);
        CheckDirectory(resultPath);
    }

    public void TryTranslate()
    {
        if (!CheckFile(origPathFile, false)) return;
        if (!CheckDirectory(resultPath, false)) return;
        if (origLanguage_YML == resultLanguage_YML)
        {
            DoDebug($"Original language equals to result language: {resultLanguage_YML}", yellow, 3);
            return;
        }

        translateButton.interactable = false;
        if (resultLanguage_YML == "-ALL-")
        {
            StartCoroutine(TranslateAll());
        }
        else
        {
            StartCoroutine(Translate(resultLanguage_YML));
        }
    }

    private IEnumerator TranslateAll()
    {
        foreach (var data in optionDatas.Where(x => x.text != "-ALL-" && x.text != origLanguage_YML))
        {
            var language = data.text;
            yield return StartCoroutine(Translate(language));
        }
    }

    private IEnumerator Translate(string resultLanguage)
    {
        translateButton.interactable = false;
        stopTranslationButton.interactable = true;
        DoDebug($"Translating to {resultLanguage}...", grey, 1);
        yield return new WaitForSeconds(0.1f);

        try
        {
            Dictionary<string, string> localized = new();

            foreach (var pair in LoadFromFileText(File.ReadAllText(origPathFile)))
            {
                localized.Add(pair.Key, Localize(pair.Value, origLanguage_YML, resultLanguage));
            }

            using (StreamWriter sw = new(resultPath + $"//{resultLanguage}.yml"))
            {
                sw.Write(SaveToStr(localized));
            }

            DoDebug($"Translation to {resultLanguage} completed", green);
        }
        catch (Exception e)
        {
            DoDebug(e.Message, red, 10);
        }

        if (translator.Error != null) DoDebug(translator.Error.Message, red, 3);

        translateButton.interactable = true;
        stopTranslationButton.interactable = false;
    }

    private bool CheckFile(string path, bool debug = true)
    {
        var exists = File.Exists(path);
        if (!exists)
        {
            if (debug) DoDebug($"File does not exist {path}", red, 3);
            return false;
        }
        else
        {
            if (debug) DoDebug($"Found file {path}", white, 3);
            PlayerPrefs.SetString("OrigPath", origPath);
            return true;
        }
    }

    private bool CheckDirectory(string path, bool debug = true)
    {
        var exists = Directory.Exists(path);
        if (!exists)
        {
            if (debug) DoDebug($"Directory does not exist {path}", red, 3);
            return false;
        }
        else
        {
            if (debug) DoDebug($"Found directory {path}", white, 3);
            PlayerPrefs.SetString("ResultPath", resultPath);
            return true;
        }
    }

    public void DoDebug(string msg, ColorType color, float clearDelay = 10)
    {
        StartCoroutine(DebugCoroutine(msg, color, clearDelay));
    }

    private IEnumerator DebugCoroutine(string msg, ColorType color, float clearDelay)
    {
        var text = $"\n<color={color}>{msg}</color>";
        debugText.text += text;
        yield return new WaitForSecondsRealtime(clearDelay);
        debugText.text = debugText.text.Replace(text, "");
    }

    public string Localize(string value, string origLanguage, string resultLanguage)
    {
        return translator.Translate(value, origLanguage, resultLanguage);
    }

    internal Dictionary<string, string> LoadFromFileText(string text)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<Dictionary<string, string>>(text);
    }

    private string SaveToStr(Dictionary<string, string> dictionary)
    {
        ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Serialize(dictionary);
    }
}
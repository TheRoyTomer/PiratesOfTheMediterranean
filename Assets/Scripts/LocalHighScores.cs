using UnityEngine;

public static class LocalHighScores
{
    public const string WavesKey = "Pirates.HighScores.Waves";
    public const string EnemiesKey = "Pirates.HighScores.Enemies";
    public const string SurvivalKey = "Pirates.HighScores.SurvivalSeconds";

    public readonly struct Records
    {
        public readonly bool Waves, Enemies, Survival;
        public Records(bool waves, bool enemies, bool survival)
        {
            Waves = waves;
            Enemies = enemies;
            Survival = survival;
        }
    }

    public static Records SaveResult(int waves, int enemies, float survival)
    {
        var records = new Records(waves > PlayerPrefs.GetInt(WavesKey, 0),
            enemies > PlayerPrefs.GetInt(EnemiesKey, 0),
            survival > PlayerPrefs.GetFloat(SurvivalKey, 0f));
        if (records.Waves) PlayerPrefs.SetInt(WavesKey, waves);
        if (records.Enemies) PlayerPrefs.SetInt(EnemiesKey, enemies);
        if (records.Survival) PlayerPrefs.SetFloat(SurvivalKey, survival);
        if (records.Waves || records.Enemies || records.Survival) PlayerPrefs.Save();
        return records;
    }
}

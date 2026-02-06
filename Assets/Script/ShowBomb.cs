using UnityEngine;
using TMPro; // TextMeshPro を使う場合

public class ShowBomb : MonoBehaviour
{
    [SerializeField] TMP_Text bombText;      // Inspector に割り当て
    [SerializeField] GameObject bombIcon;    // 任意: アイコン（非表示/表示切替用）

    void Reset()
    {
        // 便利: デフォルトで子オブジェクトの TMP_Text を拾う（あれば）
        if (bombText == null) bombText = GetComponentInChildren<TMP_Text>();
    }

    /// <summary> ボムの所持数を表示（負数は 0 表示） </summary>
    public void SetCount(int count)
    {
        if (bombText != null)
            bombText.text = "×" + Mathf.Max(0, count).ToString();

        // アイコンを非表示にしたい条件があればここで操作できる
        if (bombIcon != null) bombIcon.SetActive(count > 0);
    }
}

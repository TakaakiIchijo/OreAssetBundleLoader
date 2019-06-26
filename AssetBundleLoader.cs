using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AssetBundleLoader : MonoBehaviour
{
    private readonly List<AssetBundle> loadedAssetBundleList = new List<AssetBundle>();
    public string AssetBundleRootPath = "AssetBundles";

    /// <summary>
    ///  アセットバンドルの中身を単体ファイルで返す
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="assetName"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerator LoadAbToFile<T>(string filePath, string assetName)
    {
        if (string.IsNullOrEmpty(filePath)) yield break;

        var loadAb = LoadAb(filePath);

        yield return loadAb;

        var ab = loadedAssetBundleList.Last(); // loadAb.Current as AssetBundle;

        if (ab != null)
        {
            var abRequest = ab.LoadAssetAsync<T>(assetName);

            yield return abRequest;

            yield return abRequest.asset;
        }
    }

    /// <summary>
    ///  アセットバンドルの中身をファイルリストで返す
    /// </summary>
    /// <param name="filePath"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerator LoadAbToFileList<T>(string filePath)
    {
        Debug.Log("start ab load " + filePath);

        var loadAb = LoadAb(filePath);

        yield return loadAb;

        var ab = loadedAssetBundleList.Last();

        if (ab != null)
        {
            var abRequest = ab.LoadAllAssetsAsync<T>();

            yield return abRequest;
            yield return abRequest.allAssets;

            if (abRequest.allAssets == null)
            {
                Debug.LogError("Failed asset bundle get");
                yield break;
            }

            var loadedAssets = abRequest.allAssets;

            //Debug.Log("Asset bundle " + Path.GetFileName(filePath) +" loaded type:" + typeof(T) + " count:" + loadedAssets.Length);

            yield return loadedAssets.OfType<T>().ToList();
        }
    }

    private IEnumerator LoadAb(string filePath)
    {
        //ABファイルがメモリ上のキャッシュにあった場合はそれを返す//
        var loadedAb = loadedAssetBundleList.FirstOrDefault(asb => asb.name == filePath);

        if (loadedAb != null)
        {
            Debug.Log(filePath + " bundle file is already cashed");
            yield return loadedAb;
            yield break;
        }

        var assetBundlePath = Path.Combine(Application.streamingAssetsPath, AssetBundleRootPath,
            GetCurrentPlatFormName(), filePath);

        var task = ReadFileAsync(assetBundlePath);

        yield return new WaitUntil(() => task.IsCompleted);

        var createRequest = AssetBundle.LoadFromMemoryAsync(task.Result);

        yield return createRequest;

        if (createRequest.assetBundle == null)
        {
            Debug.LogError("Not found asset bundle " + filePath);
            yield break;
        }

        loadedAssetBundleList.Add(createRequest.assetBundle);
    }

    private async Task<byte[]> ReadFileAsync(string path)
    {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            var bytes = new byte[fs.Length];

            await fs.ReadAsync(bytes, 0, (int) fs.Length);

            return bytes;
        }
    }

    /// <summary>
    /// キャッシュしたabを個別にアンロード
    /// </summary>
    /// <param name="assetBundleName"></param>
    public void UnloadAssetBundle(string assetBundleName)
    {
        if (loadedAssetBundleList.Count == 0) return;

        var assetBundle = loadedAssetBundleList.FirstOrDefault(asb => asb.name == assetBundleName);

        if (assetBundle == null) return;

        assetBundle.Unload(true);

        loadedAssetBundleList.Remove(assetBundle);
    }

    /// <summary>
    /// キャッシュしたabをすべてアンロード
    /// </summary>
    public void UnloadAllAssetBundles()
    {
        loadedAssetBundleList.ForEach(asb => asb.Unload(true));

        loadedAssetBundleList.Clear();
    }

    private void OnDestroy()
    {
        UnloadAllAssetBundles();
    }
    
    private static string GetCurrentPlatFormName()
    {
#if UNITY_STANDALONE_WIN
        return "Windows";
#elif UNITY_STANDALONE_OSX
        return "OSX";    
#elif UNITY_ANDROID
        return "Android";
#else
        return "Windows";
#endif
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Vuforia;

public class SimpleCloudHandler : DefaultTrackableEventHandler, IObjectRecoEventHandler, ITrackableEventHandler
{
    public ImageTargetBehaviour behaviour;
    private CloudRecoBehaviour mCloudRecoBehaviour;
    private bool mIsScanning = false;
    private string mTargetMetadata = "";

    public GameObject VideoPlayer;
    public GameObject Downloading;

    // Use this for initialization 
    void Start()
    {
        // register this event handler at the cloud reco behaviour 
        mCloudRecoBehaviour = GetComponent<CloudRecoBehaviour>();

        if (mCloudRecoBehaviour)
        {
            mCloudRecoBehaviour.RegisterEventHandler(this);
        }

        VideoPlayer.SetActive(false);
        Downloading.SetActive(false);
    }

    public void OnInitialized(TargetFinder targetFinder)
    {
        Debug.Log("Cloud Reco initialized");
    }

    public void OnInitError(TargetFinder.InitState initError)
    {
        Debug.Log("Cloud Reco init error " + initError.ToString());
    }

    public void OnUpdateError(TargetFinder.UpdateState updateError)
    {
        Debug.Log("Cloud Reco update error " + updateError.ToString());
    }

    public void OnStateChanged(bool scanning)
    {
        mIsScanning = scanning;
        if (scanning)
        {
            // clear all known trackables
            VideoPlayer.SetActive(false);
            VideoPlayer.GetComponent<UnityEngine.Video.VideoPlayer>().url = "";
            VideoPlayer.GetComponent<UnityEngine.Video.VideoPlayer>().GetTargetAudioSource(0).clip = null;
            Downloading.SetActive(false);

            var tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
            tracker.GetTargetFinder<ImageTargetFinder>().ClearTrackables(false);
        }
    }

    void Update()
    {
        /* if (behaviour != null)
        {
            if (behaviour.CurrentStatusInfo == TrackableBehaviour.StatusInfo.UNKNOWN)
            {
                OnStateChanged(true);
            }else if (behaviour.CurrentStatusInfo == TrackableBehaviour.StatusInfo.NORMAL)
            {
                if (!Downloading.activeInHierarchy)
                {
                    if (!VideoPlayer.activeInHierarchy)
                    {
                        Downloading.SetActive(true);
                    }
                    else
                    {
                        Downloading.SetActive(false);
                    }
                }
            }
        }*/
    }

    // Here we handle a cloud target recognition event
    public void OnNewSearchResult(TargetFinder.TargetSearchResult targetSearchResult)
{
    GameObject newImageTarget = Instantiate(behaviour.gameObject);
    VideoPlayer = newImageTarget.transform.GetChild(0).gameObject;
    Downloading = newImageTarget.transform.GetChild(1).gameObject;
    GameObject augmentation = null;

    if (augmentation != null)
    {
        augmentation.transform.SetParent(newImageTarget.transform);
    }

    if (behaviour)
    {
        ObjectTracker tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        ImageTargetBehaviour imageTargetBehaviour = (ImageTargetBehaviour)tracker.GetTargetFinder<ImageTargetFinder>().EnableTracking(targetSearchResult, newImageTarget);
    }

    TargetFinder.CloudRecoSearchResult cloudRecoSearchResult =
        (TargetFinder.CloudRecoSearchResult)targetSearchResult;
    // do something with the target metadata
    mTargetMetadata = cloudRecoSearchResult.MetaData;

    string json = mTargetMetadata;

    ImageData thisImage = JsonUtility.FromJson<ImageData>(json);

    Debug.Log("thisImage Link: " + thisImage.Link);
    Debug.Log("thisImage Width: " + thisImage.Width);
    Debug.Log("thisImage Height: " + thisImage.Height);

    float height = thisImage.Height / thisImage.Width;

    Debug.Log("thisImage Scaled Height: " + height.ToString());

    VideoPlayer.transform.localScale = new Vector2(1, height);

    VideoPlayer.SetActive(true);
    StartCoroutine(PlayVideo(thisImage.Link));

    //StartCoroutine(hasStartedPlaying());
    // stop the target finder (i.e. stop scanning the cloud) to do this enter false, we're using true because yolo
    mCloudRecoBehaviour.CloudRecoEnabled = true;
}

IEnumerator PlayVideo(string link)
{
    VideoPlayer.GetComponent<UnityEngine.Video.VideoPlayer>().url = link.Trim();

    while (!VideoPlayer.GetComponent<UnityEngine.Video.VideoPlayer>().isPrepared)
    {
        VideoPlayer.GetComponent<MeshRenderer>().enabled = false;
        Downloading.SetActive(true);
        Debug.Log("Preparing Video");
        yield return null;
    }

    VideoPlayer.GetComponent<MeshRenderer>().enabled = true;
    Downloading.SetActive(false);
}

private void SimpleCloudHandler_prepareCompleted(UnityEngine.Video.VideoPlayer source)
{
    //Downloading.SetActive(false);
    //VideoPlayer.GetComponent<UnityEngine.Video.VideoPlayer>().targetMaterialRenderer.enabled = true;
}

IEnumerator hasStartedPlaying()
{
    if (!VideoPlayer.GetComponent<UnityEngine.Video.VideoPlayer>().isPlaying)
    {
        Downloading.SetActive(true);
        yield return new WaitForEndOfFrame();
    }
    else
    {
        Downloading.SetActive(false);
    }
}
}

public class ImageData
{
    public string Link;
    public float Width;
    public float Height;
}

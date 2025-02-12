﻿using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using VideoPlayerAsset.UI;

namespace VideoPlayerAsset.Video
{
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoController : MonoBehaviour
    {
        [SerializeField] private VideoRenderTextureUI videoRenderTextureUI;

        public VideoPlayer VideoPlayer {
            get => videoPlayerCached ??= GetComponent<VideoPlayer>();       // If not cached, cache it.
        }
        private VideoPlayer videoPlayerCached;
        private UIDocument document;

        // Loads sample video. Remove if not required.
        //void Start()
        //{
        //    // Load a sample video. 
        //    var sampleVideo = System.IO.Path.Combine(Application.streamingAssetsPath, "SampleVideo.mp4");
        //    // Check file exists.
        //    if (System.IO.File.Exists(sampleVideo))
        //        PrepareVideo(sampleVideo);
        //    else
        //        Debug.Log("Sample video not found.");
        //}

        public void PlayVideo()
        {
            VideoPlayer.Play();
        }

        public void StopVideo()
        {
            VideoPlayer.Stop();
        }

        public void PauseVideo()
        {
            VideoPlayer.Pause();
        }

        public void LoopVideo(bool loop)
        {
            if (loop)
                VideoPlayer.isLooping = true;
            else
                VideoPlayer.isLooping = false;
        }

        public void PrepareVideo(string videoPath)          // Called when a new video is selected from gallery.
        {
            VideoPlayer.url = videoPath;

            VideoPlayer.prepareCompleted += OnVideoPrepared;
            VideoPlayer.errorReceived += OnVideoError;
            VideoPlayer.Prepare();
        }

        private void OnVideoPrepared(VideoPlayer _videoPlayer)
        {
            //Debug.Log("Video prepared");
            UnsubscribeVideoEvents(_videoPlayer);

            // Make a new RenderTexture with the videos dimensions.
            RenderTexture renderTexture = new RenderTexture((int)_videoPlayer.width, (int)_videoPlayer.height, 24);
            _videoPlayer.targetTexture = renderTexture;
            videoRenderTextureUI.SetRenderTexture(renderTexture);

            // Play the video to load the first frame
            _videoPlayer.Play();

            //// Pause the video to keep the first frame displayed
            //_videoPlayer.Pause();
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError("VideoPlayer Error: " + message);
            UnsubscribeVideoEvents(vp);
        }

        private void UnsubscribeVideoEvents(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnVideoPrepared;
            vp.errorReceived -= OnVideoError;
        }

        public void Mute(bool mute)
        {
            if (mute)
                VideoPlayer.SetDirectAudioMute(0, true);
            else
                VideoPlayer.SetDirectAudioMute(0, false);
        }

        public void Scrub(double time)
        {
            // Note that if the video is looping it needs to be stopped then started again for scrub to work correctly. Possible Unity bug with video player.
            if (VideoPlayer.isLooping)
            {
                VideoPlayer.Stop();
                VideoPlayer.time = time;
                VideoPlayer.Play();
            }
            else
                VideoPlayer.time = time;
        }

        private Vector2 pixelUVResult = new Vector2(float.NaN, float.NaN);
        private void OnEnable()
        {
            document = videoRenderTextureUI.GetComponent<UIDocument>();
            document.panelSettings.SetScreenToPanelSpaceFunction(ScreenToPanelSpace);
        }

        private Vector2 ScreenToPanelSpace(Vector2 screenPosition)
        {
            // Return the pixel UV calculated in LateUpdate
            return pixelUVResult;
        }

        private void LateUpdate()
        {
            // Perform Raycast in LateUpdate
            PerformRaycast();
        }

        private void PerformRaycast()
        {
            var invalidPosition = new Vector2(float.NaN, float.NaN);

            // Create the ray manually using the camera's transform position and forward direction
            var cameraTransform = Camera.main.transform;
            var cameraRay = new Ray(cameraTransform.position, cameraTransform.forward);

            Debug.DrawRay(cameraRay.origin, cameraRay.direction * 100, Color.magenta);

            RaycastHit hit;

            // Perform Raycast using the manually created ray
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity))
            {
                pixelUVResult = invalidPosition;
                return;
            }

            // Get the texture coordinates of the hit point
            Vector2 pixelUV = hit.textureCoord;

            // Adjust pixel coordinates based on the target texture dimensions
            pixelUV.y = 1 - pixelUV.y;
            pixelUV.x *= document.panelSettings.targetTexture.width;
            pixelUV.y *= document.panelSettings.targetTexture.height;

            // Store the result to be used in the next frame when the panel needs it
            pixelUVResult = pixelUV;
        }
    }
}

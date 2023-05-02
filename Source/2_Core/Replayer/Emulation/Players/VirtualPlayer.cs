﻿using BeatLeader.Models;
using BeatLeader.Models.AbstractReplay;
using BeatLeader.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace BeatLeader.Replayer.Emulation {
    public class VirtualPlayer : MonoBehaviour {
        public class Pool : MonoMemoryPool<VirtualPlayer> {
            protected override void OnSpawned(VirtualPlayer item) {
                item.HandleInstanceSpawned();
            }
            protected override void OnDespawned(VirtualPlayer item) {
                item.HandleInstanceDespawned();
            }
        }

        [Inject] private readonly IBeatmapTimeController _beatmapTimeController = null!;
        [Inject] private readonly MenuControllersManager _menuControllersMan = null!;
        private ViewableCameraController _cameraController = null!;

        public IReplay? Replay { get; private set; }
        public IVRControllersProvider? ControllersProvider { get; private set; }

        public bool enableInterpolation = true;

        private LinkedListNode<PlayerMovementFrame>? _lastProcessedNode;
        private LinkedList<PlayerMovementFrame>? _frames;
        private bool _allowPlayback;

        public float lerpMultiplier = 0.0f;

        public void Init(IReplay replay, IVRControllersProvider provider) {
             Replay = replay;
            ControllersProvider = provider;
            _frames = new(replay.PlayerMovementFrames);
            _lastProcessedNode = _frames.First;
            _allowPlayback = true;
            gameObject.SetActive(true);
        }

        private SerializablePose Ctrl2Pose(VRController c) {
            return new SerializablePose(
                new SerializableVector3(c.position),
                new SerializableQuaternion(c.rotation)
            );
        }

        private SerializablePose PoseInverse(SerializablePose p, Vector3 pos, Quaternion rot) {
            return new SerializablePose(
                p.position - pos,
                p.rotation * Quaternion.Inverse(rot)
            );
        }

        internal void SetCameraController(ViewableCameraController cameraController) {
            _cameraController = cameraController;
        }

        private void PlayFrame(LinkedListNode<PlayerMovementFrame>? frame) {
            if (frame?.Next == null) return;
            _lastProcessedNode = frame;

            var currentFrame = frame.Value;
            var leftSaberPose = currentFrame.leftHandPose;
            var rightSaberPose = currentFrame.rightHandPose;
            var headPose = currentFrame.headPose;

            if (enableInterpolation) {
                float t = (_beatmapTimeController.SongTime - frame.Value.time) /
                    (frame.Next.Value.time - frame.Value.time);

                var nextFrame = frame.Next.Value;
                leftSaberPose = leftSaberPose.Lerp(nextFrame.leftHandPose, t);
                rightSaberPose = rightSaberPose.Lerp(nextFrame.rightHandPose, t);
                headPose = headPose.Lerp(nextFrame.headPose, t);
            }

            var offsetPos = Vector3.zero;
            var offsetRot = Quaternion.identity;
            if (_cameraController != null && _cameraController.SelectedView != null) {
                offsetPos = _cameraController.CameraContainer.position;
                offsetRot = _cameraController.CameraContainer.rotation;
            }

            // share control between replay and menu controllers / real hands
            var ctrlRH = PoseInverse(Ctrl2Pose(_menuControllersMan.RightHand), offsetPos, offsetRot);
            var ctrlLH = PoseInverse(Ctrl2Pose(_menuControllersMan.LeftHand), offsetPos, offsetRot);

            rightSaberPose = rightSaberPose.Lerp(ctrlRH, lerpMultiplier);
            leftSaberPose = leftSaberPose.Lerp(ctrlLH, lerpMultiplier);

            ControllersProvider!.LeftSaber.transform.SetLocalPose(leftSaberPose);
            ControllersProvider.RightSaber.transform.SetLocalPose(rightSaberPose);
            ControllersProvider.Head.transform.SetLocalPose(headPose);
        }

        private void Update() {
            if (_allowPlayback && TryGetFrameByTime(_lastProcessedNode!,
                _beatmapTimeController.SongTime, out var frame)) {
                PlayFrame(frame?.Previous);
            }
        }

        private void HandleInstanceSpawned() {
            _beatmapTimeController.SongWasRewoundEvent += HandleSongWasRewound;
        }

        private void HandleInstanceDespawned() {
            gameObject.SetActive(false);
            _allowPlayback = false;
            _beatmapTimeController.SongWasRewoundEvent -= HandleSongWasRewound;
            ControllersProvider = null;
            _lastProcessedNode = null;
            _frames = null;
        }

        private void HandleSongWasRewound(float time) {
            _lastProcessedNode = _frames!.First;
        }
        
        private static bool TryGetFrameByTime(LinkedListNode<PlayerMovementFrame> entryPoint,
            float time, out LinkedListNode<PlayerMovementFrame>? frame) {
            for (frame = entryPoint; frame != null; frame = frame.Next) {
                if (frame.Value.time >= time) return true;
            }
            frame = null;
            return false;
        }
    }
}
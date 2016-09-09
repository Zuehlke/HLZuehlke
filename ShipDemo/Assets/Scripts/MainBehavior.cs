using System;
using UnityEngine;
using System.Collections;
using UnityEngine.VR.WSA.Input;
using HoloToolkit.Unity;
using UnityEngine.Windows.Speech;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MainBehavior : MonoBehaviour {

    public Material occluderMaterial;
    public Material wireframeMaterial;

    private enum InteractionMode {
        Translate,
        Rotate,
        Scale
    }

    private GestureRecognizer _gestureRecognizer;

    private KeywordRecognizer _keywordRecognizer;
    Dictionary<string, Action> _keywords = new Dictionary<string, Action>();

    private bool _dragging;
    private Vector3 _lastPosition;

    private InteractionMode _mode = InteractionMode.Translate;
    private GameObject _marker;

    void Start() {
        _marker = GameObject.Find("Marker");

        //SpatialMappingManager.Instance.enabled = true;

        _gestureRecognizer = new GestureRecognizer();
        _gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.ManipulationTranslate);
        _gestureRecognizer.TappedEvent += OnAirTap;
        _gestureRecognizer.ManipulationStartedEvent += HoldStarted;
        _gestureRecognizer.ManipulationUpdatedEvent += GestureMoved;
        _gestureRecognizer.ManipulationCompletedEvent += HoldCompleted;
        _gestureRecognizer.ManipulationCanceledEvent += HoldCompleted;
        _gestureRecognizer.StartCapturingGestures();

        SetupSpeechKeywords();
        _keywordRecognizer = new KeywordRecognizer(_keywords.Keys.ToArray());

        // Register a callback for the KeywordRecognizer and start recognizing!
        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        _keywordRecognizer.Start();
    }

    // Update is called once per frame
    void Update() {

    }

    private void SetupSpeechKeywords()
    {
        _keywords.Add("scale", SwitchToResizeMode);
        _keywords.Add("resize", SwitchToResizeMode);
        _keywords.Add("translate", SwitchToTranslateMode);
        _keywords.Add("move", SwitchToTranslateMode);
        _keywords.Add("rotate", SwitchToRotateMode);
        _keywords.Add("show grid", () => SpatialMappingManager.Instance.SetSurfaceMaterial(wireframeMaterial));
        _keywords.Add("hide grid", () => SpatialMappingManager.Instance.SetSurfaceMaterial(occluderMaterial));
    }

    private void SwitchToResizeMode()
    {
        _mode = InteractionMode.Scale;
    }

    private void SwitchToTranslateMode()
    {
        _mode = InteractionMode.Translate;
    }

    private void SwitchToRotateMode()
    {
        _mode = InteractionMode.Rotate;
    }

    void OnDestroy()
    {
        _gestureRecognizer.TappedEvent -= OnAirTap;
        _gestureRecognizer.ManipulationStartedEvent -= HoldStarted;
        _gestureRecognizer.ManipulationUpdatedEvent -= GestureMoved;
        _gestureRecognizer.ManipulationCompletedEvent -= HoldCompleted;
        _gestureRecognizer.ManipulationCanceledEvent -= HoldCompleted;
    }

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Action keywordAction;
        if (_keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }

    private void HoldStarted(InteractionSourceKind source, Vector3 position, Ray headRay)
    {
        _dragging = true;
        _lastPosition = position;
    }

    private void GestureMoved(InteractionSourceKind source, Vector3 position, Ray headray)
    {
        if (_dragging)
        {
            switch (_mode)
            {
                case InteractionMode.Translate:
                    gameObject.transform.position += (position - _lastPosition) * 2;
                    //PlaceMarker();
                    break;
                case InteractionMode.Rotate:
                    gameObject.transform.Rotate(Vector3.up, (position.x - _lastPosition.x) * 360);
                    break;
                case InteractionMode.Scale:
                    gameObject.transform.localScale *= 1 + (position.y - _lastPosition.y) * 5;
                    break;
            }
            _lastPosition = position;
        }
    }

    private void PlaceMarker() {
        RaycastHit hitInfo;
        if (Physics.Raycast(gameObject.transform.position + new Vector3(0, 10, 0), Vector3.down, out hitInfo, 30f, 0))
        {
            _marker.GetComponent<MeshRenderer>().enabled = false;
            var basePoint = hitInfo.point;
            _marker.transform.position = basePoint;
        }
        else
        {
            _marker.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void SnapToPlaneWithOffset(float offset)
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(gameObject.transform.position + new Vector3(0, 10, 0), Vector3.down, out hitInfo, 30f, 0))
        {
            var basePoint = hitInfo.point;
            gameObject.transform.position = basePoint + new Vector3(0, offset, 0);
        }
    }

    private void HoldCompleted(InteractionSourceKind source, Vector3 position, Ray headRay)
    {
        _dragging = false;
        //if (_mode == InteractionMode.Translate) {
        //    SnapToPlaneWithOffset(0.3f);
        //}
        // marker is no longer used
        //_marker.GetComponent<MeshRenderer>().enabled = false;
    }

    private void OnAirTap(InteractionSourceKind source, int tapcount, Ray headray)
    {
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        var objectRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.right);
        var distance1m = gazeDirection / gazeDirection.magnitude;
        Vector3 spawnPosition = headPosition + distance1m;
        gameObject.transform.position = spawnPosition;
    }
}

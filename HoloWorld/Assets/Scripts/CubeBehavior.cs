using UnityEngine;
using UnityEngine.VR.WSA.Input;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows.Speech;
using System;

public class CubeBehavior : MonoBehaviour
{
    private readonly bool STICKY = false;

    private MeshRenderer _meshRenderer;
    private GestureRecognizer _gestureRecognizer;

    private KeywordRecognizer _keywordRecognizer;
    Dictionary<string, Action> _keywords = new Dictionary<string, Action>();

    private bool _dragging;
    private Vector3 _lastPosition;

    private bool _physicsActive;

    private MeshRenderer _markerFrameRenderer;

    void Start()
    {
        _meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        _meshRenderer.enabled = false;

        var markerFrame = GameObject.Find("CubeFrame");
        _markerFrameRenderer = markerFrame.GetComponent<MeshRenderer>();
        _markerFrameRenderer.enabled = false;
        
        _gestureRecognizer = new GestureRecognizer();
        _gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.ManipulationTranslate);
        _gestureRecognizer.TappedEvent += OnAirTap;
        _gestureRecognizer.ManipulationStartedEvent += HoldStarted;
        _gestureRecognizer.ManipulationUpdatedEvent += GestureMoved;
        _gestureRecognizer.ManipulationCompletedEvent += HoldCompleted;
        _gestureRecognizer.ManipulationCanceledEvent += HoldCompleted;
        _gestureRecognizer.StartCapturingGestures();

        SpatialMapping.Instance.SetMappingEnabled(true);

        SetupSpeechKeywords();
        _keywordRecognizer = new KeywordRecognizer(_keywords.Keys.ToArray());

        // Register a callback for the KeywordRecognizer and start recognizing!
        _keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        _keywordRecognizer.Start();
    }

    private void SetupSpeechKeywords()
    {
        _keywords.Add("drop", () =>
        {
            ActivatePhysics();
            _physicsActive = true;
        });
        _keywords.Add("show grid", () => SpatialMapping.Instance.DrawVisualMeshes = true);
        _keywords.Add("hide grid", () => SpatialMapping.Instance.DrawVisualMeshes = false);
    }

    void OnDestroy()
    {
        _gestureRecognizer.TappedEvent -= OnAirTap;
        _gestureRecognizer.ManipulationStartedEvent -= HoldStarted;
        _gestureRecognizer.ManipulationUpdatedEvent -= GestureMoved;
        _gestureRecognizer.ManipulationCompletedEvent -= HoldCompleted;
        _gestureRecognizer.ManipulationCanceledEvent -= HoldCompleted;
    }

    void Update()
    {

    }

    private void OnAirTap(InteractionSourceKind source, int tapcount, Ray headray)
    {
        PlaceCube();
    }

    private void PlaceCube()
    {

        DeactivatePhysics();
        _physicsActive = false;

        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        var objectRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.right);
        Vector3 spawnPosition;

        RaycastHit hitInfo;

        if (STICKY && Physics.Raycast(headPosition, gazeDirection, out hitInfo, 30f, SpatialMapping.PhysicsRaycastMask))
        {
            var basePoint = hitInfo.point;
            spawnPosition = basePoint + (hitInfo.normal * gameObject.transform.localScale.x * 0.5f);
            objectRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
        }
        else
        {
            var distance1m = gazeDirection / gazeDirection.magnitude;
            spawnPosition = headPosition + distance1m;
        }

        gameObject.transform.position = spawnPosition;
        gameObject.transform.rotation = objectRotation;

        _meshRenderer.enabled = true;
    }

    private void HoldStarted(InteractionSourceKind source, Vector3 position, Ray headRay)
    {
        _dragging = true;
        _markerFrameRenderer.enabled = true;
        DeactivatePhysics();
        _lastPosition = position;
    }

    private void GestureMoved(InteractionSourceKind source, Vector3 position, Ray headray)
    {
        if (_dragging)
        {
            gameObject.transform.position += (position - _lastPosition) * 2;
            _lastPosition = position;
        }
    }

    private void HoldCompleted(InteractionSourceKind source, Vector3 position, Ray headRay)
    {
        _dragging = false;
        _markerFrameRenderer.enabled = false;
        if (_physicsActive)
        {
            ActivatePhysics();
        }
        
    }

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Action keywordAction;
        if (_keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }

    private void DeactivatePhysics()
    {
        var rigidBody = gameObject.GetComponent<Rigidbody>();
        if (rigidBody != null)
        {
            Destroy(rigidBody);
        }
    }

    private void ActivatePhysics()
    {
        if (!gameObject.GetComponent<Rigidbody>())
        {
            var rigidBody = gameObject.AddComponent<Rigidbody>();
            rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
}



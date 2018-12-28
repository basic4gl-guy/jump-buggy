using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Speedometer : MonoBehaviour {

    [Header("Wireup")]
    public Rigidbody Car;
    public Text Readout;

    [Header("Settings")]
    public float MaxSpeedMPH = 200.0f;

    [Header("Bars")]
    public int MaxBars = 20;
    public float BarGap = 10.0f;
    public float BarStartHeight = 80.0f;
    public float BarEndHeight = 200.0f;
    public float SineCurveHeight = 20.0f;
    public int HighlightNBars = 5;
    public Color BarColor = new Color(1.0f, 1.0f, 0.0f);
    public Color DimBarColor = new Color(0.5f, 0.5f, 0.0f);
    public Color HighlightBarColor = new Color(1.0f, 1.0f, 0.5f);
    public Color DimHighlightBarColor = new Color(0.75f, 0.75f, 0.5f);

    public void Start()
    {
    }

    public void OnRenderObject()
    {
        Draw(false);
    }

    public void OnDrawGizmos()
    {
        Draw(true);
    }

    public void Update()
    {
        if (Readout != null)
        {
            Readout.text = Speed.ToString("0");
        }
    }

    private void Draw(bool isEditor)
    {
        // Calculate some things
        float mphPerBar = MaxSpeedMPH / MaxBars;
        var rect = GetComponent<RectTransform>();
        float spacing = (rect.rect.width - BarGap) / MaxBars;
        float barWidth = spacing - BarGap;
        int litCount = Mathf.FloorToInt(Speed / mphPerBar);        

        // Create and apply material
        var lineMaterial = DrawingUtil.CreateLineMaterial();
        lineMaterial.SetPass(0);

        // Render in canvas space
        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        GL.Begin(GL.QUADS);

        for (int i = 0; i < MaxBars; i++)
        {
            GL.Color((i + 1) % HighlightNBars != 0
                        ? (i >= litCount ? DimBarColor : BarColor)
                        : (i >= litCount ? DimHighlightBarColor : HighlightBarColor));

            float x = i * spacing - rect.rect.width / 2;
            float leftF = (i * spacing) / rect.rect.width;
            float rightF = (i * spacing + barWidth) / rect.rect.width;
            float leftCurveAdj = Mathf.Sin(leftF * Mathf.PI) * SineCurveHeight;
            float rightCurveAdj = Mathf.Sin(rightF * Mathf.PI) * SineCurveHeight;
            float leftHeight = BarStartHeight + (BarEndHeight - BarStartHeight) * leftF;
            float rightHeight = BarStartHeight + (BarEndHeight - BarStartHeight) * rightF;
            GL.Vertex3(x, leftCurveAdj + leftHeight, 0.0f);
            GL.Vertex3(x + barWidth, rightCurveAdj + rightHeight, 0.0f);
            GL.Vertex3(x + barWidth, rightCurveAdj - rightHeight, 0.0f);
            GL.Vertex3(x, leftCurveAdj - leftHeight, 0.0f);
        }

        GL.End();

        GL.PopMatrix();
    }

    private float Speed
    {
        get
        {
            if (!Application.isPlaying) return 100.0f;

            if (Car == null) return 0.0f;

            float metersPerSecond = Mathf.Max(Car.transform.worldToLocalMatrix.MultiplyVector(Car.velocity).z, 0.0f);
            float kmPerHour = (metersPerSecond * 3600.0f) / 1000.0f;
            return kmPerHour * MathUtil.MilesPerKM;
        }
    }
}

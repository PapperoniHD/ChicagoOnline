using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextAnimator : MonoBehaviour
{
    public float waveHeight = 10f;
    public float waveSpeed = 2f;
    public float waveFrequency = 2f;

    private TextMeshProUGUI tmp;
    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    // Start is called before the first frame update
    void Start()
    {
        tmp.ForceMeshUpdate();
        textInfo = tmp.textInfo;

        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] = textInfo.meshInfo[i].vertices.Clone() as Vector3[];
        }
    }

    void Update()
    {
        tmp.ForceMeshUpdate(); 
        textInfo = tmp.textInfo;

        float time = Time.time * waveSpeed;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // Use the original position
            Vector3 offset = originalVertices[materialIndex][vertexIndex];

            float waveOffset = Mathf.Sin(time + i * waveFrequency) * waveHeight;

            for (int j = 0; j < 4; j++)
            {
                Vector3 original = originalVertices[materialIndex][vertexIndex + j];
                vertices[vertexIndex + j] = original + new Vector3(0, waveOffset, 0);
            }
        }

        // Push the modified vertex data to the mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            tmp.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}

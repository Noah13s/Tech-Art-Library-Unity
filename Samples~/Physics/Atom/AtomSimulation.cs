using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Atom))]
[RequireComponent(typeof(ElementKnowledge))]
public class AtomSimulation : MonoBehaviour
{
    private Atom atom;
    private ElementKnowledge elementKnowledge;
    // Start is called before the first frame update
    void Start()
    {
        atom = GetComponent<Atom>();
        elementKnowledge = GetComponent<ElementKnowledge>();
    }


}

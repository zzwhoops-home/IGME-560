using System.Collections.Generic;
using UnityEngine;



//------------------------------------------------------------------------
//
//  class to handle the training data
//------------------------------------------------------------------------
public class Data
{

    // //total number of built in patterns
    public const int NUM_PATTERNS = 16;

    //how many vectors each pattern contains
    public const int NUM_VECTORS = 12;

    //these will contain the training set when created.
    List<List<double>> m_SetIn = new List<List<double>>();
    List<List<double>> m_SetOut = new List<List<double>>();

    public List<List<double>> GetInputSet()
    {
        return m_SetIn;
    }

    public List<List<double>> GetOutputSet()
    {
        return m_SetOut;
    }

    //the names of the gestures
    List<string> m_vecNames;

    //the vectors which make up the gestures
    List<List<double>> m_vecPatterns;

    //number of patterns loaded into database
    int m_iNumPatterns;

    //size of the pattern vector
    int m_iVectorSize;

    //------------------------------------------------------------------------
    //
    //  constant training data
    //------------------------------------------------------------------------
    public readonly double[,] InputVectors= {
        {
            1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0
        }, //right

        {
            -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0
        }, //left

        {
            0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1, 0,1
        }, //up

        {
            0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1, 0,-1
        }, //down

        {
            1,0, 1,0, 1,0, 0,-1, 0,-1, 0,-1, -1,0, -1,0, -1,0, 0,1, 0,1, 0,1
        }, //clockwise square

        {
            -1,0, -1,0, -1,0, 0,-1, 0,-1, 0,-1, 1,0, 1,0, 1,0, 0,1, 0,1, 0,1
        }, //anticlockwise square

        {
            1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, 1,0, -0.45f,0.9f, -0.9f, 0.45f, -0.9f,0.45f
        }, //Right Arrow 

        {
            -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, -1,0, 0.45f,0.9f, 0.9f, 0.45f, 0.9f,0.45f
        }, //Left Arrow

        {
            -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, 
            -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f,
        }, // north-west
        {
            0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 
            0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f,
        }, // north-east
        {
            1,0, 1,0, 1,0, 1,0, -0.72f,0.81f,-0.7f,0.72f,0.59f,0.69f, 1,0, 1,0, 1,0, 1,0, 1,0
        }, //zorro
        {
            0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f,
            0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f
        }, // south-east
        {
            -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f,
            -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f
        }, // south-west
        {
            -0.309f,-0.951f, -0.309f,-0.951f, -0.309f,-0.951f, 0.809f,0.588f, 0.809f,0.588f, -1.0f,0.0f, -1.0f,0.0f, 0.809f,-0.588f, 0.809f,-0.588f,
            -0.309f,0.951f, -0.309f,0.951f, -0.309f,0.951f
        }, // star
        {
            0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f, 0.7f,-0.7f,
            -1,0, -1,0, -1,0, -1,0,
            0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f, 0.7f,0.7f
        }, // triangle cw
        {
            -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f, -0.7f,-0.7f,
            1,0, 1,0, 1,0, 1,0,
            -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f, -0.7f,0.7f
        } // triangle ccw
    };

public readonly string[] Names = {
        "Right",
        "Left",
        "Up",
        "Down",
        "Clockwise Square",
        "Anti-Clockwise Square",
        "Right Arrow",
        "Left Arrow",
        "North West",
        "North East",
        "Zorro",
        "South East",
        "South West",
        "Star",
        "Clockwise Triangle",
        "Anti-Clockwise Triangle"
    };


    public Data(int NumStartPatterns,
        int VectorSize)
    {
        m_vecNames = new List<string>();
        m_vecPatterns = new List<List<double>>();
        m_SetIn = new List<List<double>>();
        m_SetOut = new List<List<double>>();
        m_iNumPatterns = NumStartPatterns;
        m_iVectorSize = VectorSize;
        Init();
        CreateTrainingSetFromData();
    }

    //--------------------------------- Init ---------------------------------
    //
    //  Initializes the appropriate vectors with the const training data
    //------------------------------------------------------------------------
    void Init()
    {
        //for each const pattern  
        for (int ptn = 0; ptn < m_iNumPatterns; ++ptn)
        {
            //add it to the vector of patterns
            List<double> temp = new List<double>(m_iVectorSize * 2);

            for (int v = 0; v < m_iVectorSize * 2; ++v)
            {
                temp.Add(InputVectors[ptn,v]);
            }
            
            m_vecPatterns.Add(temp);

            //add the name of the pattern
            m_vecNames.Add(Names[ptn]);
        }
    }

    //------------------------- PatternName ----------------------------------
    //
    //  returns the pattern name at the given value
    //------------------------------------------------------------------------
    public string PatternName(int val)
    {
        if (m_vecNames.Count > 0)
        {
            return m_vecNames[val];
        }

        else
        {
            return "";
        }
    }

    //------------------------- AddData -------------------------------------
    //
    //  adds a new pattern to data
    //-----------------------------------------------------------------------
    public bool AddData(List<double> data, string NewName)
    {
        //check that the size is correct
        if (data.Count != m_iVectorSize*2)
        {
            Debug.Log("Error: Incorrect Data Size");
            return false;
        }

        //add the name
        m_vecNames.Add(NewName);

        //add the data
        m_vecPatterns.Add(data);

        //keep a track of number of patterns
        ++m_iNumPatterns;

        //create the new training set
        CreateTrainingSetFromData();

        return true; 
    }

    // --------------------------- CreateTrainingSetFromData -----------------
    //
    //  this function creates a training set from the data defined as constants
    //  in CData.h. 
    //------------------------------------------------------------------------
    public void CreateTrainingSetFromData()
    {
        //empty the vectors
        m_SetIn.Clear();
        m_SetOut.Clear();

        //add each pattern
        for (int ptn = 0; ptn < m_iNumPatterns; ++ptn)
        {
            //add the data to the training set
            m_SetIn.Add(m_vecPatterns[ptn]);

            //create the output vector for this pattern. First fill a 
            //std::vector with zeros
            List<double> outputs = new List<double>(m_iNumPatterns);
            for (int i = 0; i < m_iNumPatterns; ++i)
            {
                outputs.Add(0);
            }

            //set the relevant output to 1
            outputs[ptn] = 1;

            //add it to the output set
            m_SetOut.Add(outputs);
        }

    }
}

using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This is a pretty direct program translation from the RecognizeIt C++ program by
/// Mat Buckland done in 2002.
/// </summary>
public class GameManager : MonoBehaviour
{
    ///////////////////////
    // constants
    ///////////////////////
    // set window width and height
    public const int WINDOW_WIDTH = 400;
    public const int WINDOW_HEIGHT = 400;
    public const int NUM_HIDDEN_NEURONS = 8;
    //the learning rate for the backprop.
    public const double LEARNING_RATE    =   0.5;
    //output has to be above this value for the program
    //to agree on a pattern. Below this value and it
    //will try to guess the pattern
    public const double MATCH_TOLERANCE = 0.96;
    
    ///////////////////////
    // var's for drawing gestures with the mouse
    ///////////////////////
    private bool gestureReady = true;
    private List<Vector3> positions = new List<Vector3>();
    private LineRenderer lineRenderer;
    
    ///////////////////////
    // GUI Var's
    ///////////////////////
    public TMP_Text classificationTextBox;
    public TMP_Text errorTextBox;
    
    ///////////////////////
    // var's for the neural net
    ///////////////////////
    enum mode
    {
        LEARNING,
        ACTIVE,
        UNREADY,
        TRAINING
    }
    //the neural network
    NeuralNet m_pNet;
    //this class holds all the training data
    Data m_pData;
    //the user mouse gesture paths - raw and smoothed
    List<Vector2> m_vecPath = new List<Vector2>();
    List<Vector2> m_vecSmoothPath = new List<Vector2>();
    //the smoothed path transformed into vectors
    List<double> m_vecVectors = new List<double>();
    //true if user is gesturing
    bool m_bDrawing;
    //the highest output the net produces. This is the most
    //likely candidate for a matched gesture.
    double m_dHighestOutput;
    //the best match for a gesture based on m_dHighestOutput
    int m_iBestMatch;
    //if the network has found a pattern this is the match
    int m_iMatch;
    //the raw mouse data is smoothed to this number of points
    int m_iNumSmoothPoints;
    //the number of patterns in the database;
    int m_iNumValidPatterns;
    //the current state of the program
    mode m_Mode;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // set up the line renderer for this example
        // Add a LineRenderer component
        this.transform.position = Vector3.zero;
        lineRenderer = this.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.positionCount = 100;
        lineRenderer.SetPosition(0, new Vector3(0, 0, 0));
        lineRenderer.loop = false;
        
        // set up the neural net var's
        m_vecPath = new List<Vector2>();
        m_vecVectors = new List<double>();
        m_bDrawing = false;
        m_iNumSmoothPoints = Data.NUM_VECTORS + 1;
        m_dHighestOutput = 0;
        m_iBestMatch = -1;
        m_iMatch = -1;
        m_iNumValidPatterns = Data.NUM_PATTERNS;
        m_Mode = mode.UNREADY;
        //create the database
        m_pData = new Data(m_iNumValidPatterns, Data.NUM_VECTORS);
        //setup the network
        m_pNet = new NeuralNet(Data.NUM_VECTORS * 2, //inputs
            m_iNumValidPatterns, //outputs
            GameManager.NUM_HIDDEN_NEURONS, //hidden
            GameManager.LEARNING_RATE);
        m_pNet.setErrorTextBox(errorTextBox);
        
        // train the network in the very beginning with preexisting data:
        TrainNetwork();
        
        // tmp
        m_Mode = mode.ACTIVE;
    }

    // Update is called once per frame
    void Update()
    {
        if (gestureReady && Input.GetMouseButtonDown(0))
        {
            // left click down
            Vector3 mousePos = Input.mousePosition;
            
            // because it can be infinity depending on the window var's
            if (!Single.IsInfinity(mousePos.x))
            {
                // clear positions for a new gesture
                positions.Clear();
                m_vecPath.Clear();
                gestureReady = false;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                Vector3 mouseTransform = new Vector2((worldPos.x/GameManager.WINDOW_WIDTH) - 1.0f, 
                    (worldPos.y/GameManager.WINDOW_HEIGHT) - 1.0f);
                m_vecPath.Add(mouseTransform);
                positions.Add(new Vector3(worldPos.x, worldPos.y, 0));
                Debug.Log(mousePos);
            }
        } 
        else if (Input.GetMouseButtonUp(0))
        {
            gestureReady = true;
            // left click up
            Vector3 mousePos = Input.mousePosition;
            //Vector3 mouseTransform = new Vector2((mousePos.x/MainProgram.WINDOW_WIDTH) - 1.0f, (mousePos.y/MainProgram.WINDOW_HEIGHT) - 1.0f);

            if (!Single.IsInfinity(mousePos.x))
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                positions.Add(new Vector3(worldPos.x, worldPos.y, 0));
                Vector3 mouseTransform = new Vector2((worldPos.x/GameManager.WINDOW_WIDTH) - 1.0f, 
                    (worldPos.y/GameManager.WINDOW_HEIGHT) - 1.0f);
                m_vecPath.Add(mouseTransform);
                //lineRenderer.positionCount = positions.Count;
                //lineRenderer.SetPositions(positions.ToArray());
                Debug.Log(mousePos);
                
                // now smooth the collected data and show it
                if (Smooth())
                {
                    //create the vectors
                    CreateVectors();
                    
                    if (m_Mode == mode.ACTIVE)
                    {

                        if (TestForMatch() && m_iMatch >= 0)
                        {
                            Debug.Log("Match found is: " +m_pData.Names[m_iMatch] + " with number " + m_iMatch);
                            classificationTextBox.text = "Classifiction: " + m_pData.Names[m_iMatch];
                        }
                        else
                        {
                            Debug.Log("Best match was: " + m_iBestMatch);
                            classificationTextBox.text = "None - Closest was " + m_pData.Names[m_iBestMatch];
                        }
                        
                    }
                }
            }
        }
        else if (!gestureReady)
        {
            // we're currently recording a gesture
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            positions.Add(new Vector3(worldPos.x, worldPos.y, 0));
            Vector3 mouseTransform = new Vector2((worldPos.x/GameManager.WINDOW_WIDTH) - 1.0f, 
                (worldPos.y/GameManager.WINDOW_HEIGHT) - 1.0f);
            m_vecPath.Add(mouseTransform);
        }
    }
    
    //call this to add a point to the mouse path
    public void AddPoint(Vector2 p)
    {
        m_vecPath.Add(p);
    }

    //--------------------------- Clear --------------------------------------
    //
    //  clears the current data
    //------------------------------------------------------------------------
    public void Clear()
    {
        m_vecPath.Clear();
        m_vecSmoothPath.Clear();
        m_vecVectors.Clear();
    }

    //----------------------------------- CreateVectors ----------------------
//
//  this function creates normalized vectors out of the series of POINTS
//  in m_vecSmoothPoints
//------------------------------------------------------------------------
    public void CreateVectors()
    {
        Debug.Log("CreateVectors: " + m_vecSmoothPath.Count);
        m_vecVectors.Clear();
        for (int p=1; p<m_vecSmoothPath.Count; ++p)
        {    
            // Note: technically, the mouse position is a float as 
            // returned by input
            // we normalize and then convert to doubles
            // for the neural network training
            float x = m_vecSmoothPath[p].x - m_vecSmoothPath[p-1].x;
            float y = m_vecSmoothPath[p].y - m_vecSmoothPath[p-1].y;

            Vector2 v1 = new Vector2(1, 0);
            Vector2 v2 = new Vector2(x, y);
            v2.Normalize();
            double2 doubl2V2Norm = new double2(v2.x, v2.y);

            m_vecVectors.Add(doubl2V2Norm.x);
            m_vecVectors.Add(doubl2V2Norm.y);
        }
    }

    //------------------------------------- Smooth ---------------------------
    //
    //  Smooths the mouse data as described in chapter 9
    //------------------------------------------------------------------------
    public bool Smooth()
    {
        //make sure it contains enough points for us to work with
        if (m_vecPath.Count < m_iNumSmoothPoints)
        {
            //return
            Debug.Log("There aren't enough mouse position points to classify.");
            return false;
        }

        //copy the raw mouse data
        m_vecSmoothPath = m_vecPath;

        //while there are excess points iterate through the points
        //finding the shortest spans, creating a new point in its place
        //and deleting the adjacent points.
        while (m_vecSmoothPath.Count > m_iNumSmoothPoints)
        {
            double ShortestSoFar = 99999999f;

            int PointMarker = 0;

            //calculate the shortest span
            for (int SpanFront=2; SpanFront<m_vecSmoothPath.Count-1; ++SpanFront)
            {
                //calculate the distance between these points
                double length = 
                    System.Math.Sqrt( (m_vecSmoothPath[SpanFront-1].x - m_vecSmoothPath[SpanFront].x) *
                          (m_vecSmoothPath[SpanFront-1].x - m_vecSmoothPath[SpanFront].x) +

                          (m_vecSmoothPath[SpanFront-1].y - m_vecSmoothPath[SpanFront].y)*
                          (m_vecSmoothPath[SpanFront-1].y - m_vecSmoothPath[SpanFront].y));

                if (length < ShortestSoFar)
                {
                    ShortestSoFar = length;

                    PointMarker = SpanFront;
                }      
            }

            //now the shortest span has been found calculate a new point in the 
            //middle of the span and delete the two end points of the span
            Vector2 newPoint;

            newPoint.x = (m_vecSmoothPath[PointMarker-1].x + 
                          m_vecSmoothPath[PointMarker].x)/2;

            newPoint.y = (m_vecSmoothPath[PointMarker-1].y +
                          m_vecSmoothPath[PointMarker].y)/2;

            m_vecSmoothPath[PointMarker-1] = newPoint;
            m_vecSmoothPath.RemoveAt(PointMarker);
        }
        
        // show the resulting line drawn
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
        return true;
    }

    //------------------------- TestForMatch ----------------------------------
//
//  checks the mouse pattern to see if it matches one of the learned
//  patterns
//-------------------------------------------------------------------------
    public bool TestForMatch()
    {
        Debug.Log("TestForMatch: " + m_vecVectors.Count);
        //input the smoothed mouse vectors into the net and see if we get a match
        List<double> outputs = m_pNet.Update(m_vecVectors);

        if (outputs.Count == 0)
        {
            Debug.Log("Error in with ANN output");
            return false;
        }

//run through the outputs and see which is highest
        m_dHighestOutput = 0;
        m_iBestMatch = 0;
        m_iMatch = -1;

        for (int i = 0; i < outputs.Count; ++i)
        {
            if (outputs[i] > m_dHighestOutput)
            {
                //make a note of the most likely candidate
                m_dHighestOutput = outputs[i];

                m_iBestMatch = i;


                //if the candidates output exceeds the threshold we 
                //have a match! ...so make a note of it.
                if (m_dHighestOutput > GameManager.MATCH_TOLERANCE)
                {
                    m_iMatch = m_iBestMatch;
                }
            }
        }

        return true;
    }


//------------------------------ TrainNetwork -----------------------------
//
//  Trains the neural net work with the predefined training set
//-------------------------------------------------------------------------
  public bool TrainNetwork()
  {
      m_Mode = mode.TRAINING;

      if(!m_pNet.Train(m_pData))
      {
          return false;
      }

      Debug.Log("Training is successful.");
      m_Mode = mode.ACTIVE;
      return true;
  }

  //renders the mouse gestures and relevant data such as the number
  //of training epochs and training error
  void Render(int cxClient, int cyClient) {
//       //render error from any training taking place
// if(m_Mode == TRAINING)
// {   
//   string s = "Error: " + ftos(m_pNet->Error());
//   TextOut(surface, cxClient/2, 5, s.c_str(), s.size());
//
//    s = "Epochs: " + ftos(m_pNet->Epoch());
//   TextOut(surface, 5, 5, s.c_str(), s.size());
// }
//
// if (m_pNet->Trained())
// {
//   if ((m_Mode == ACTIVE))
//   {
//      string s = "Recognition circuits active";
//      TextOut(surface, 5, cyClient-20, s.c_str(), s.size());
//   }
//
//   if (m_Mode == LEARNING)
//   {
//     string s = "Recognition circuits offline - Enter a new gesture";
//     TextOut(surface, 5, cyClient-20, s.c_str(), s.size());
//   }
// }
//
// else
// {
//    string s = "Training in progress...";
//    TextOut(surface, 5, cyClient-20, s.c_str(), s.size());
// }
//
//
// if (!m_bDrawing)
// {  
//   //render best match
//   if (m_dHighestOutput > 0)
//   {
//  
//     if ( (m_vecSmoothPath.size() > 1) && (m_Mode != LEARNING) )
//     {
//       if (m_dHighestOutput < MATCH_TOLERANCE)
//       {
//         string s = "I'm guessing this is the pattern " + 
//                    m_pData->PatternName(m_iBestMatch); 
//
//         TextOut(surface, 5, 10, s.c_str(), s.size());
//       }
//
//       else
//       {
//         SetTextColor(surface, RGB(0, 0, 255));
//
//         string s = m_pData->PatternName(m_iMatch);
//         TextOut(surface, 5, 10, s.c_str(), s.size());
//
//         SetTextColor(surface, RGB(0, 0, 0));
//
//       }
//     }
//
//     else if (m_Mode != LEARNING)
//     {
//       SetTextColor(surface, RGB(255, 0, 0));
//
//       string s = "Not enough points drawn - plz try again";
//       TextOut(surface, 5, 10, s.c_str(), s.size());
//
//       SetTextColor(surface, RGB(0, 0, 0));
//     }
//   }
// }
//
// if (m_vecPath.size() < 1)
// {
//   return;
// }
//
// MoveToEx(surface, m_vecPath[0].x, m_vecPath[0].y, NULL);
//
// for (int vtx=1; vtx<m_vecPath.size(); ++vtx)
// {
//   LineTo(surface, m_vecPath[vtx].x, m_vecPath[vtx].y);
// }
//
// //draw the points which make up the smoothed path
// if ((!m_bDrawing) && (m_vecSmoothPath.size() > 0))
// {
//   for (int vtx=0; vtx<m_vecPath.size(); ++vtx)
//   {
//     POINTS pt = m_vecSmoothPath[vtx];
//
//     Ellipse(surface, pt.x-2, pt.y-2, pt.x+2, pt.y+2);
//   }
// }	 
  }

  //returns whether or not the mouse is currently drawing
  public bool Drawing()
  {
      return m_bDrawing;
  }

  //------------------------------ Drawing ---------------------------------
  //
//  this is called whenever the user depresses or releases the right
//  mouse button.
//  If val is true then the right mouse button has been depressed so all
//  mouse data is cleared ready for the next gesture. If val is false a
//  gesture has just been completed. The gesture is then either added to
//  the current data set or it is tested to see if it matches an existing
//  pattern.
//  The hInstance is required so a dialog box can be created as a child
//  window of the main app instance. The dialog box is used to grab the
//  name of any user defined gesture
//------------------------------------------------------------------------
    public bool Drawing(bool val)
    {
        if (val)
        {
            Clear();
        }

        else
        {
            //smooth and vectorize the data if we have enough points
            if (Smooth())
            {
                //create the vectors
                CreateVectors();

                if (m_Mode == mode.ACTIVE)
                {

                    if (!TestForMatch())
                    {
                        return false;
                    }
                }

                else
                {
                    // //add the data set if user is happy with it
                    // if(MessageBox(m_hwnd, "Happy with this gesture?", "OK?", MB_YESNO) == IDYES)
                    // {
                    //     //grab a name for this pattern
                    //     DialogBox(hInstance,
                    //         MAKEINTRESOURCE(IDD_DIALOG1),
                    //         m_hwnd,
                    //         DialogProc);
                    //
                    //
                    //     //add the data
                    //     m_pData->AddData(m_vecVectors, m_sPatternName);
                    //
                    //     //delete the old network
                    //     delete m_pNet;
                    //
                    //     ++m_iNumValidPatterns;
                    //
                    //     //create a new network
                    //     m_pNet = new CNeuralNet(NUM_VECTORS*2,
                    //         m_iNumValidPatterns,
                    //         NUM_VECTORS*2,
                    //         LEARNING_RATE);
                    //
                    //     //train the network
                    //     TrainNetwork();
                    //
                    //     m_Mode = ACTIVE;
                    // }
                    //
                    // else
                    // {
                    //     //clear dismissed gesture
                    //     m_vecPath.clear();
                    // }
                }
            }
        }
   
        m_bDrawing = val;

        return true;
    }

//--------------------------------- LearningMode -------------------------
//
//  clears the screen and puts the app into learning mode, ready to accept
//  a user defined gesture
//------------------------------------------------------------------------
    public void LearningMode()
    {
        m_Mode = mode.LEARNING;
        Clear();
        //update window
        // [WORK - still necessary?????]
        //InvalidateRect(m_hwnd, NULL, TRUE);
        //UpdateWindow(m_hwnd);
    }

}

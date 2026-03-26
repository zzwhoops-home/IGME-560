using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

//-------------------------------------------------------------------
//	define neuron struct
//-------------------------------------------------------------------
public struct SNeuron
{
    //the number of inputs into the neuron
    public int				      m_iNumInputs;
    //the weights for each input
    public List<double>	m_vecWeight ;
    //the activation of this neuron
    public double          m_dActivation;
    //the error value
    public double          m_dError;

    public SNeuron(int NumInputs)
    {
	    m_iNumInputs = NumInputs+1;
	    m_dActivation = 0;
	    m_dError = 0;
	    //we need an additional weight for the bias hence the +1
	    m_vecWeight = new List<double>(NumInputs+1);
	    for (int i=0; i<NumInputs+1; ++i)
	    {
		    //set up the weights with an initial random value
		    m_vecWeight.Add(Utilities.RandomClamped());
	    }
    }
}

//---------------------------------------------------------------------
//	struct to hold a layer of neurons.
//---------------------------------------------------------------------
public struct SNeuronLayer
{
	//the number of neurons in this layer
	public int					      m_iNumNeurons;
	//the layer of neurons
	public List<SNeuron>		m_vecNeurons;

	//-----------------------------------------------------------------------
	//	ctor creates a layer of neurons of the required size by calling the 
	//	SNeuron ctor the rqd number of times
	//-----------------------------------------------------------------------
	public SNeuronLayer(int NumNeurons, 
		int NumInputsPerNeuron)
	{
		m_iNumNeurons = NumNeurons;
		m_vecNeurons = new List<SNeuron>(NumNeurons);
		for (int i=0; i<NumNeurons; ++i)
			m_vecNeurons.Add(new SNeuron(NumInputsPerNeuron));
	}
}

//----------------------------------------------------------------------
//	neural net class
//----------------------------------------------------------------------
//
public class NeuralNet
{
	int m_iNumInputs;
	int m_iNumOutputs;
	int m_iNumHiddenLayers;
	int m_iNeuronsPerHiddenLyr;

	//we must specify a learning rate for backprop
	double m_dLearningRate;

	//cumulative error for the network (sum (outputs - expected))
	double m_dErrorSum;

	//true if the network has been trained
	bool m_bTrained;

	//epoch counter
	int m_iNumEpochs;

	//storage for each layer of neurons including the output layer
	List<SNeuronLayer> m_vecLayers = new List<SNeuronLayer>();

	public const double BIAS = -1.0;
	public const double ACTIVATION_RESPONSE = 1.0;
	//when the total error is below this value the 
	//backprop stops training
	public const double ERROR_THRESHOLD  =   0.003;

	private TMP_Text errorRateTextBox;

	public void setErrorTextBox(TMP_Text text)
	{
		errorRateTextBox = text;
	}
	
//----------------------------NetworkTrainingEpoch -----------------------
//
//  given a training set this method trains the network using backprop.
//  The training sets comprise of series of input vectors and a series
//  of output vectors.
//  Returns false if there is a problem
//------------------------------------------------------------------------
bool NetworkTrainingEpoch(ref List<List<double>> SetIn,
                                     ref List<List<double>> SetOut)
{
  //this will hold the cumulative error value for the training set
  m_dErrorSum = 0;
  
  //run each input pattern through the network, calculate the errors and update
  //the weights accordingly
  for (int vec=0; vec<SetIn.Count; ++vec)
  {
    //first run this input vector through the network and retrieve the outputs
    List<double> outputs = Update(SetIn[vec]);
  
    //return if error has occurred
    if (outputs.Count == 0)
    {
      return false;
    }

    //for each output neuron calculate the error and adjust weights
    //accordingly
    for (int op=0; op<m_iNumOutputs; ++op)
    {
      //first calculate the error value
      double err = (SetOut[vec][op] - outputs[op]) * outputs[op]
                                                   * (1 - outputs[op]);     
  
      //keep a record of the error value
      SNeuron tmpNeuron = m_vecLayers[1].m_vecNeurons[op];
      tmpNeuron.m_dError = err;
  
      //update the SSE. (when this value becomes lower than a
      //preset threshold we know the training is successful)
      m_dErrorSum += (SetOut[vec][op] - outputs[op]) *
                     (SetOut[vec][op] - outputs[op]); 
  
      //curWeight = m_vecLayers[1].m_vecNeurons[op].m_vecWeight.begin();
      //curNrnHid = m_vecLayers[0].m_vecNeurons.begin();
  
      //for each weight up to but not including the bias
      List<double> tmpWeights = m_vecLayers[1].m_vecNeurons[op].m_vecWeight;
      for (int curWeightCount = 0; curWeightCount < tmpWeights.Count-1; ++curWeightCount)
      {
        //calculate the new weight based on the backprop rules
        tmpWeights[curWeightCount] += err * m_dLearningRate * 
                                      m_vecLayers[0].m_vecNeurons[curWeightCount].m_dActivation;
      }
  
      //and the bias for this neuron
      // double check that this is right??? [WORK]
      tmpWeights[tmpWeights.Count-1] += err * m_dLearningRate * BIAS;     
    }
  
	//**moving backwards to the hidden layer**
    List<SNeuron> tmpHidden = m_vecLayers[0].m_vecNeurons;
  
    int n = 0;
    
    //for each neuron in the hidden layer calculate the error signal
    //and then adjust the weights accordingly
    for (int tmpHiddenCount=0; tmpHiddenCount<tmpHidden.Count; ++tmpHiddenCount)
    { 
	    double err = 0;
  
      List<SNeuron> tmpOut = m_vecLayers[1].m_vecNeurons;
  
      //to calculate the error for this neuron we need to iterate through
      //all the neurons in the output layer it is connected to and sum
      //the error * weights
      for (int outputCount=0; outputCount<tmpOut.Count; ++outputCount)
      {
        err += tmpOut[outputCount].m_dError * tmpOut[outputCount].m_vecWeight[n];
      }
  
      //now we can calculate the error
      err *= tmpHidden[tmpHiddenCount].m_dActivation * (1 - tmpHidden[tmpHiddenCount].m_dActivation);     
      
      //for each weight in this neuron calculate the new weight based
      //on the error signal and the learning rate
      for (int w=0; w<m_iNumInputs; ++w)
      {
        //calculate the new weight based on the backprop rules
       tmpHidden[tmpHiddenCount].m_vecWeight[w] += err * m_dLearningRate * SetIn[vec][w];
      }
  
      //and the bias
      tmpHidden[tmpHiddenCount].m_vecWeight[m_iNumInputs] += err * m_dLearningRate * BIAS;
      ++n;
    }
  
  }//next input vector
  return true;
}


	//------------------------------createNet()------------------------------
	//
	//	this method builds the ANN. The weights are all initially set to
	//	random values -1 < w < 1
	//------------------------------------------------------------------------
	void CreateNet()
	{
		if (m_vecLayers==null) 
			m_vecLayers = new List<SNeuronLayer>(m_iNumHiddenLayers);
		else
		{
			m_vecLayers.Clear();
		}
		
		//create the layers of the network
		if (m_iNumHiddenLayers > 0)
		{
			//create first hidden layer
			m_vecLayers.Add(new SNeuronLayer(m_iNeuronsPerHiddenLyr, m_iNumInputs));
    
			for (int i=0; i<m_iNumHiddenLayers-1; ++i)
			{

				m_vecLayers.Add(new SNeuronLayer(m_iNeuronsPerHiddenLyr,
					m_iNeuronsPerHiddenLyr));
			}

			//create output layer
			m_vecLayers.Add(new SNeuronLayer(m_iNumOutputs, m_iNeuronsPerHiddenLyr));
		}

		else
		{
			//create output layer
			m_vecLayers.Add(new SNeuronLayer(m_iNumOutputs, m_iNumInputs));
		}
	}

	//--------------------------- Initialize ---------------------------------
	//
	//  randomizes all the weights to values btween 0 and 1
	//------------------------------------------------------------------------
	//sets all the weights to small random values
	void InitializeNetwork()
	{
		//for each layer
		for (int i=0; i<m_iNumHiddenLayers + 1; ++i)
		{
			//for each neuron
			for (int n=0; n<m_vecLayers[i].m_iNumNeurons; ++n)
			{
				//for each weight
				for (int k=0; k<m_vecLayers[i].m_vecNeurons[n].m_iNumInputs; ++k)
				{
					m_vecLayers[i].m_vecNeurons[n].m_vecWeight[k] = Utilities.RandomClamped();
				}
			}
		}

		m_dErrorSum  = 9999;
		m_iNumEpochs = 0;

	}

	//-------------------------------Sigmoid function-------------------------
	//
	//------------------------------------------------------------------------
	public double Sigmoid(double netinput,double response)
	{
		return ( 1 / ( 1 + System.Math.Exp(-netinput / response)));
	}

	public NeuralNet(int NumInputs,
		int NumOutputs,
		int HiddenNeurons,
		double LearningRate)
	{
		m_iNumInputs = NumInputs;
		m_iNumOutputs = NumOutputs;
		m_iNumHiddenLayers = 1;
		m_iNeuronsPerHiddenLyr = HiddenNeurons;
		m_dLearningRate = LearningRate;
		m_dErrorSum = 9999;
		m_bTrained = false;
		m_iNumEpochs = 0;
		CreateNet();
	}


	//-------------------------------Update-----------------------------------
	//
	//	given an input vector this function calculates the output vector
	//
	//------------------------------------------------------------------------
	public List<double> Update(List<double> inputs)
	{
		// stores the resultant outputs from each layer
		List<double> outputs = new List<double>(inputs.Count);
		List<double> tmpInputs = new List<double>(inputs.Count);
		for (int tmpIndex = 0; tmpIndex < inputs.Count; tmpIndex++)
		{
			tmpInputs.Add(inputs[tmpIndex]);
		}
		
		int cWeight = 0;
	
		//first check that we have the correct amount of inputs
		if (inputs.Count != m_iNumInputs)
		{
			Debug.Log("Number of inputs is: " + m_iNumInputs + " but the array for inputs is of size " + inputs.Count);
			//just return an empty vector if incorrect.
			return outputs;
		}
		
		//For each layer...
		for (int i=0; i<m_iNumHiddenLayers + 1; ++i)
		{
			if ( i > 0 )
			{
				tmpInputs.Clear();
				for (int tmpIndex = 0; tmpIndex < outputs.Count; tmpIndex++)
				{
					tmpInputs.Add(outputs[tmpIndex]);
				}
			}
			outputs.Clear();
			cWeight = 0;

			//for each neuron sum the (inputs * corresponding weights).Throw 
			//the total at our sigmoid function to get the output.
			for (int n=0; n<m_vecLayers[i].m_iNumNeurons; ++n)
			{
				double netinput = 0;

				int	NumInputs = m_vecLayers[i].m_vecNeurons[n].m_iNumInputs;
				
				//for each weight
				for (int k=0; k<NumInputs - 1; ++k)
				{
					//sum the weights x inputs
					netinput += m_vecLayers[i].m_vecNeurons[n].m_vecWeight[k] * 
					            tmpInputs[cWeight++];
				}

				//add in the bias
				netinput += m_vecLayers[i].m_vecNeurons[n].m_vecWeight[NumInputs-1] * 
				            BIAS;

	 
				//The combined activation is first filtered through the sigmoid 
				//function and a record is kept for each neuron 
				SNeuron neuron = m_vecLayers[i].m_vecNeurons[n];
				double tmpActivation = Sigmoid(netinput, ACTIVATION_RESPONSE);
				neuron.m_dActivation = tmpActivation;
				m_vecLayers[i].m_vecNeurons[n] = neuron;

				//store the outputs from each layer as we generate them.
				outputs.Add(m_vecLayers[i].m_vecNeurons[n].m_dActivation);

				cWeight = 0;
			}
		}

		return outputs;
	}

	//----------------------------- Train ------------------------------------
	//
	//  Given some training data in the form of a CData object this function
	//  trains the network until the error is within acceptable limits.
	// 
	//  the HWND is required to give some graphical feedback
	//------------------------------------------------------------------------
	public bool Train(Data data)
	{
	
		List<List<double> > SetIn  = data.GetInputSet();
		List<List<double> > SetOut = data.GetOutputSet();
	
		//first make sure the training set is valid
		if ((SetIn.Count     != SetOut.Count)  || 
		    (SetIn[0].Count  != m_iNumInputs)   ||
		    (SetOut[0].Count != m_iNumOutputs))
		{
			Debug.Log("Error: Inputs != Outputs");
			return false;
		}
  
		//initialize all the weights to small random values
		InitializeNetwork();
	
		//train using backprop until the SSE is below the user defined
		//threshold
		
		// NOTE: since the network might not converge, we
		// put a halt on how long it might run
		int howManyTimes = 200000;
		while( m_dErrorSum > ERROR_THRESHOLD && (howManyTimes>0))
		{
			//return false if there are any problems
			if (!NetworkTrainingEpoch(ref SetIn, ref SetOut))
			{
				return false;
			}
			++m_iNumEpochs;
			howManyTimes--;
		}
	
		Debug.Log("error is currently: " + m_dErrorSum);
		errorRateTextBox.text = "Error: " + m_dErrorSum;
		m_bTrained = true;
   
		return true;
	}
	
}

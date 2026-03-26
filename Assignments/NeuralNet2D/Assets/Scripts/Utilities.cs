using Unity.Mathematics.Geometry;
using UnityEngine;

public static class Utilities
{
    static int	  iset = 0;
    static float gset = 0;
    
    //returns a random integer between x and y inclusive
    public static int RandInt(int x,int y)
    {
        return Random.Range(x, y);
    }

    //returns a random float in the range -1 < n < 1
    public static float RandomClamped()
    {
        return Random.Range(-0.999f, 0.999f);
    }

    public static float RandFloat()
    {
        return Random.Range(0f, 0.999f);
    }
    
    //Gaussian distribution
    public static float RandGauss()
    {
        float fac = 0, rsq = 0, v1 = 0, v2 = 0;
	
        if (iset==0) 
        {
            do 
            {
                v1=2.0f*RandFloat()-1.0f;
                v2=2.0f*RandFloat()-1.0f;
                rsq=v1*v1+v2*v2;
            } 
            while (rsq>=1.0 || rsq==0.0);
		
            fac= (float) System.Math.Sqrt(-2.0*System.Math.Log(rsq)/rsq);
            gset=v1*fac;
            iset=1;
            return v2*fac;
        }
        else 
        {
            iset = 0;
            return gset;
        }
    }
    
    //-------------------------------------Clamp()-----------------------------------------
    //
    //	clamps the first argument between the second two
    //
    //-------------------------------------------------------------------------------------
    public static void Clamp(ref double arg, double min, double max)
    {
        if (arg < min)
        {
            arg = min;
        }

        if (arg > max)
        {
            arg = max;
        }
    }
}

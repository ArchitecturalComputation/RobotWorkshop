
rectangles [] allRects = new rectangles [25];
float [][]   distance = new float [600][300];
float minDistance=10000000000L;
float maxDistance=0;
int numOfBricks=1;
int xNext=0;
int yNext=0;
void setup()
{
  size(700, 400);
  allRects[0]=new rectangles(radians(random(0, 180)), int(random(0, 700)), int(random(0, 400)));
  allRects[0].display();
}

void draw()
{ 
  for (int i=50; i<width-50; ++i)
  {
    for (int j=50; j<height-50; ++j)
    {
      for (int k=0; k<numOfBricks; ++k)
      {
        if (pow((i-allRects[k].xValue), 2)+pow((j-allRects[k].yValue), 2)<minDistance)
        {
          minDistance=(pow((i-allRects[k].xValue), 2)+pow((j-allRects[k].yValue), 2));
        }
      }
      distance[i-50][j-50]=minDistance;
      minDistance=10000000000L;
    }    
  }
  
  for (int i=50; i<width-50; ++i)
  {
    for (int j=50; j<height-50; ++j)
    {
      if(distance[i-50][j-50]>maxDistance)
      {
        maxDistance=distance[i-50][j-50];
        xNext=i;
        yNext=j;
      }
    }
  }
  
  allRects[numOfBricks]= new rectangles(atan2((yNext-allRects[numOfBricks-1].yValue), (xNext-allRects[numOfBricks-1].xValue)), xNext, yNext);
  allRects[numOfBricks].display();

  if (numOfBricks<24)
  {
    ++numOfBricks;
    xNext=0;
    yNext=0;
    maxDistance=0;
  }
  else
  {
    noLoop();
  }
  delay(1000);
}
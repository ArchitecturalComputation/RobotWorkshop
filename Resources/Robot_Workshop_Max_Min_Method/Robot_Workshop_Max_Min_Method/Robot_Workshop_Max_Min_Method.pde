int nextRect;
int padding = 100;
int numRect = 15;
int numInit = 3;
Rectangle[] recs;

void setup()
{
  size(1400, 800);
  rectMode(CENTER);
  recs = new Rectangle[numRect];
  randomSeed(12);
  initRects(numInit);
}

void initRects(int _numInit)
{
  for (int i = 0; i < _numInit; i++)
  {
    recs[i] = new Rectangle(random(padding, width - padding), random(padding, height - padding), random(0, PI));
  }
  nextRect = _numInit;
}

void draw()
{
  background(255);
  for (Rectangle r : recs)
  {
    if (r != null) r.display();
  }  
}

void keyPressed()
{
  if (nextRect < numRect)
  {
    int newX = 0, newY = 0; //<>//
    float maxDist = 0; //maximum minimum distance to bricks for all pixels :)))
    for (int i = padding; i < width - padding; i++)
    {
      for (int j = padding; j < height - padding; j++)
      {
        float minDist = width + height;
        for (Rectangle r : recs)  if (r != null)
        {
          float d = sqrt( pow((r.x-i), 2) + pow((r.y-j), 2) );
          if (d<minDist) 
          {
            minDist = d;
          }
        }
        
        if (minDist > maxDist)
        {
          maxDist = minDist;
          newX = i;
          newY = j;
        }
      }
    }
  
    float angle = atan2((newY - recs[nextRect-1].y), (newX - recs[nextRect-1].x));
    recs[nextRect] = new Rectangle(newX, newY, angle);
    
    nextRect++;
    println(nextRect);  
  } else println("Max number of rectangles reached!");  
}
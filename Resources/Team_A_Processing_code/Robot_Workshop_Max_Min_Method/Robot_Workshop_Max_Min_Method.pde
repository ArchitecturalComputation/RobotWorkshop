int nextRect;
int padding = 100;
int numRect = 15;
int numInit = 2;
Rectangle[] recs;

void setup()
{
  size(1400, 800);
  rectMode(CENTER);
  recs = new Rectangle[numRect];
  randomSeed(1);
  initRects(numInit);
}

void initRects(int _numInit)
{
  //for (int i = 0; i < _numInit; i++)
  //{
  //  recs[i] = new Rectangle(random(padding, width - padding), random(padding, height - padding), random(0, PI));
  //}
  recs[0] = new Rectangle(400, 500, 0);
  recs[1] = new Rectangle(800, 300, PI/2);
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
    println(String.format("Block number: %d, %d, %d, %f", nextRect, newX, newY, angle));  
  } else println("Max number of rectangles reached!");  
}
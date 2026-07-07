import { TestBed } from '@angular/core/testing';
import { InterceptorService } from './interceptor'; 

describe('InterceptorService', () => {
  let interceptor: InterceptorService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        InterceptorService
      ]
    });

    interceptor = TestBed.inject(InterceptorService);
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });
});
